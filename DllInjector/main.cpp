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

	for (int i = 1; i < argc; i++)
	{
		auto dllPath = argv[i];
		if (DoesFileExist(dllPath)) {
			Injection::DllInjector injector;
			injector.InjectDll(DIVA_PROCESS_NAME, dllPath);
		}
	}

	return EXIT_SUCCESS;
}
