using System.Collections.Generic;

namespace K39C
{
    public class DivaPatcher
    {
        Settings Settings;
        Manipulator Manipulator;
        Dictionary<long, byte[]> patches;

        public DivaPatcher(Manipulator manipulator, Settings settings)
        {
            patches = new Dictionary<long, byte[]>
            {
		        // Disable the keychip time bomb
		        { 0x0000000140210820, new byte [] { 0xB8, 0x00, 0x00, 0x00, 0x00, 0xC3 } },
		        // Just completely ignore all SYSTEM_STARTUP errors
		        { 0x00000001403F5080, new byte [] { 0xC3 } },
		        // Always exit TASK_MODE_APP_ERROR on the first frame
		        { 0x00000001403F73A7, new byte [] { 0x90, 0x90 } },
                { 0x00000001403F73C3, new byte [] { 0x89, 0xD1, 0x90 } },
		        // Ignore the EngineClear variable to clear the framebuffer at all resolutions
		        { 0x0000000140501480, new byte [] { 0x90, 0x90 } },
                { 0x0000000140501515, new byte [] { 0x90, 0x90 } },
		        // Don't update the touch slider state so we can write our own
		        // { 0x000000014061579B, new byte [] { 0x90, 0x90, 0x90, 0x8B, 0x42, 0xE0, 0x90, 0x90, 0x90 } },
		        // Write ram files to the current directory instead of Y:/SBZV/ram
		        { 0x000000014066CF09, new byte [] { 0xE9, 0xD8, 0x00 } },
		        // Change mdata path from "C:/Mount/Option" to "mdata/"
		        // { 0x0000000140A8CA18, new byte [] { 0x6D, 0x64, 0x61, 0x74, 0x61, 0x2F, 0x00 } },
                // { 0x000000014066CEAE, new byte [] { 0x06 } },
		        // *But of course we have a valid keychip*, return true
		        { 0x000000014066E820, new byte [] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 } },
		        // Skip parts of the network check state
		        { 0x00000001406717B1, new byte [] { 0xE9, 0x22, 0x03, 0x00 } },
		        // Set the initial DHCP WAIT timer value to 0
		        { 0x00000001406724E7, new byte [] { 0x00, 0x00 } },
		        // Ignore SYSTEM_STARTUP Location Server checks
		        { 0x00000001406732A2, new byte [] { 0x90, 0x90 } },
		        // Toon Shader Fix by lybxlpsv
		        { 0x000000014050214F, new byte [] { 0x90, 0x90 } },
		        // Toon Shader Outline Fix by lybxlpsv
		        { 0x0000000140641102, new byte [] { 0x01 } },
            };
            Manipulator = manipulator;
            Settings = settings;
        }

        public void ApplyPatches()
        {
            foreach (var patch in patches)
                Manipulator.WritePatch(patch.Key, patch.Value);
            if (Settings.FreePlay)
                // Always return true for the SelCredit enter SelPv check
                Manipulator.WritePatch(0x0000000140393610, new byte[] { 0xB0, 0x01, 0xC3, 0x90, 0x90, 0x90 });
            if (Settings.GlutCursor != GlutCursor.NONE)
                // Use GLUT_CURSOR_RIGHT_ARROW instead of GLUT_CURSOR_NONE
                Manipulator.WritePatch(0x000000014019341B, new byte[] { (byte)Settings.GlutCursor });
            if (Settings.HideCredits)
            {
                // Dirty hide of CREDIT(S) counter, revised by rakisaionji
                Manipulator.WritePatch(0x00000001403BAC2E, new byte[] { 0xD8 }); // CREDIT(S)
                Manipulator.WritePatch(0x00000001403BABEF, new byte[] { 0x06, 0xB6 }); // FREE PLAY
            }
        }
    }
}
