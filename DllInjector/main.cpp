#include "DllInjector.h"
#include <string>

const std::string DIVA_PROCESS_NAME = "diva.exe";

int GetDirectorySeperatorPosition(std::string path)
{
	for (int i = path.size() - 1; i >= 0; i--)
	{
		auto currentChar = path[i];
		if (currentChar == '\\' || currentChar == '/')
			return i;
	}
	return -1;
}

std::string GetModuleDirectory()
{
	HMODULE module = GetModuleHandleW(NULL);
	CHAR modulePathBuffer[MAX_PATH];
	GetModuleFileName(module, modulePathBuffer, MAX_PATH);

	auto modulePath = std::string(modulePathBuffer);
	int seperatorPos = GetDirectorySeperatorPosition(modulePath);

	if (seperatorPos != -1)
		return std::string(modulePathBuffer).substr(0, seperatorPos);

	return 0;
}

COORD GetConsoleCursorPosition(HANDLE hConsoleOutput)
{
	CONSOLE_SCREEN_BUFFER_INFO cbsi;
	GetConsoleScreenBufferInfo(hConsoleOutput, &cbsi);
	return cbsi.dwCursorPosition;
}

bool DoesFileExist(std::string filePath)
{
	auto fileAttrib = GetFileAttributes(filePath.c_str());
	return fileAttrib != INVALID_FILE_ATTRIBUTES;
}

int main(int argc, char *argv[])
{
	if (argc < 2) return EXIT_FAILURE;

	auto moduleDirectory = GetModuleDirectory();
	for (int i = 1; i < argc; i++)
	{
		auto dllPath = moduleDirectory + "/" + argv[i];
		if (DoesFileExist(dllPath)) {
			Injection::DllInjector injector;
			injector.InjectDll(DIVA_PROCESS_NAME, dllPath);
		}
	}

	return EXIT_SUCCESS;
}
