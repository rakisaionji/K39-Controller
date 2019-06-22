#include "Windows.h"
#include "GameState.h"

const int updatesPerFrame = 39;
GameState currentGameState;
GameState previousGameState;
bool dataInitialized = false;

constexpr uint64_t ENGINE_UPDATE_HOOK_TARGET_ADDRESS = 0x000000014005D440;
constexpr uint64_t CURRENT_GAME_STATE_ADDRESS = 0x0000000140CEFAA0;
constexpr uint64_t UPDATE_TASKS_ADDRESS = 0x000000014006C570;
constexpr uint64_t DATA_INIT_STATE_ADDRESS = 0x0000000140CEFA58;
constexpr uint64_t SYSTEM_WARNING_ELAPSED_ADDRESS = (0x0000000140E67D90 + 0x68);

void *InstallHook(void *source, void *destination, int length)
{
	const DWORD minLen = 0xE;

	if (length < minLen)
		return NULL;

	BYTE stub[] =
	{
		0xFF, 0x25, 0x00, 0x00, 0x00, 0x00, // jmp qword ?ptr [$+6]
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 // ptr???
	};

	void *trampoline = VirtualAlloc(0, length + sizeof(stub), MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

	DWORD oldProtect;
	VirtualProtect(source, length, PAGE_EXECUTE_READWRITE, &oldProtect);

	DWORD64 returnAddress = (DWORD64)source + length;

	// trampoline
	memcpy(stub + 6, &returnAddress, 8);

	memcpy((void *)((DWORD_PTR)trampoline), source, length);
	memcpy((void *)((DWORD_PTR)trampoline + length), stub, sizeof(stub));

	// orig
	memcpy(stub + 6, &destination, 8);
	memcpy(source, stub, sizeof(stub));

	for (int i = minLen; i < length; i++) *(BYTE *)((DWORD_PTR)source + i) = 0x90;

	VirtualProtect(source, length, oldProtect, &oldProtect);

	return (void *)((DWORD_PTR)trampoline);
}

void UpdateTick()
{
	if (dataInitialized)
		return;

	previousGameState = currentGameState;
	currentGameState = *(GameState *)CURRENT_GAME_STATE_ADDRESS;

	if (currentGameState == GS_STARTUP)
	{
		typedef void UpdateTask();
		UpdateTask *updateTask = (UpdateTask *)UPDATE_TASKS_ADDRESS;

		// speed up TaskSystemStartup
		for (int i = 0; i < updatesPerFrame; i++)
			updateTask();

		constexpr int DATA_INITIALIZED = 3;

		// skip TaskDataInit
		*(int *)(DATA_INIT_STATE_ADDRESS) = DATA_INITIALIZED;

		// skip TaskWarning
		*(int *)(SYSTEM_WARNING_ELAPSED_ADDRESS) = 3939;
	}
	else if (previousGameState == GS_STARTUP)
	{
		dataInitialized = true;
	}
}

void InstallHooks()
{
	InstallHook((void *)ENGINE_UPDATE_HOOK_TARGET_ADDRESS, (void *)UpdateTick, 0xE);
}

extern "C" __declspec(dllexport) bool WINAPI DllMain(HINSTANCE hInstDll, DWORD fdwReason, LPVOID lpvReserved)
{
	switch (fdwReason)
	{
	case DLL_PROCESS_ATTACH:
		InstallHooks();
		break;
	case DLL_PROCESS_DETACH:
		break;
	case DLL_THREAD_ATTACH:
		break;
	case DLL_THREAD_DETACH:
		break;
	}
	return true;
}
