#include "DllInjector.h"

namespace Injection
{
	int DllInjector::GetProcessID(const std::string &processName)
	{
		HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
		PROCESSENTRY32 structprocsnapshot = { 0 };

		structprocsnapshot.dwSize = sizeof(PROCESSENTRY32);

		if (snapshot == INVALID_HANDLE_VALUE || Process32First(snapshot, &structprocsnapshot) == FALSE)
			return NULL;

		while (Process32Next(snapshot, &structprocsnapshot))
		{
			if (!strcmp(structprocsnapshot.szExeFile, processName.c_str()))
			{
				CloseHandle(snapshot);
				return structprocsnapshot.th32ProcessID;
			}
		}
		CloseHandle(snapshot);
		return NULL;
	}

	bool DllInjector::InjectDll(const int &processId, const std::string &dllPath)
	{
		long dllSize = dllPath.length() + 1;
		HANDLE hProc = OpenProcess(PROCESS_ALL_ACCESS, FALSE, processId);

		if (hProc == NULL) return false;

		LPVOID myAlloc = VirtualAllocEx(hProc, NULL, dllSize, MEM_COMMIT, PAGE_EXECUTE_READWRITE);
		if (myAlloc == NULL) return false;

		int isWriteOK = WriteProcessMemory(hProc, myAlloc, dllPath.c_str(), dllSize, 0);
		if (isWriteOK == 0) return false;

		DWORD threadId;
		LPTHREAD_START_ROUTINE addrLoadLibrary = (LPTHREAD_START_ROUTINE)GetProcAddress(LoadLibrary("kernel32"), "LoadLibraryA");
		HANDLE threadReturn = CreateRemoteThread(hProc, NULL, 0, addrLoadLibrary, myAlloc, 0, &threadId);

		if (threadReturn == NULL) return false;

		if ((hProc != NULL) && (myAlloc != NULL) && (isWriteOK != ERROR_INVALID_HANDLE) && (threadReturn != NULL)) return true;

		return false;
	}

	bool DllInjector::InjectDll(const std::string &processName, const std::string &dllPath)
	{
		return InjectDll(GetProcessID(processName), dllPath);
	}
}
