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
                { 0x00000001400E7730, new byte [] { 0xB8, 0x00, 0x00, 0x00, 0x00, 0xC3 } },
                // Just completely ignore all SYSTEM_STARTUP errors
                { 0x0000000140290590, new byte [] { 0xC3 } },
                // Always exit TASK_MODE_APP_ERROR on the first frame
                { 0x0000000140292567, new byte [] { 0x90, 0x90 } },
                { 0x0000000140292583, new byte [] { 0x89, 0xD1, 0x90 } },
                // Ignore the EngineClear variable to clear the framebuffer at all resolutions
                { 0x000000014037E1A0, new byte [] { 0x90, 0x90 } },
                { 0x000000014037E235, new byte [] { 0x90, 0x90 } },
                // Don't update the touch slider state so we can write our own
                // { 0x000000014045003B, new byte [] { 0x90, 0x90, 0x90, 0x8B, 0x42, 0xE0, 0x90, 0x90, 0x90 } },
                // Write ram files to the current directory instead of Y:/SBZV/ram
                { 0x0000000140499CF9, new byte [] { 0xE9, 0xD8, 0x00 } },
                // *But of course we have a valid keychip*, return true
                { 0x000000014049B830, new byte [] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 } },
                // Skip parts of the network check state
                { 0x000000014049F600, new byte [] { 0xE9, 0x2B, 0x03, 0x00 } },
                // Set the initial DHCP WAIT timer value to 0
                { 0x00000001404A034E, new byte [] { 0x00, 0x00 } },
                // Ignore SYSTEM_STARTUP Location Server checks
                { 0x00000001404A0F73, new byte [] { 0x90, 0x90 } },
                // Toon Shader Fix
                { 0x000000014037ED40, new byte [] { 0x90, 0x90 } },
                // Toon Shader Outline Fix
                { 0x0000000140479AE2, new byte [] { 0x01 } },
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
                Manipulator.WritePatch(0x000000014024E170, new byte[] { 0xB0, 0x01, 0xC3, 0x90, 0x90, 0x90 });
            if (Settings.GlutCursor != GlutCursor.NONE)
                // Use GLUT_CURSOR_RIGHT_ARROW instead of GLUT_CURSOR_NONE
                Manipulator.WritePatch(0x0000000140063BCB, new byte[] { (byte)Settings.GlutCursor });
            if (Settings.HideCredits)
            {
                // Dirty hide of CREDIT(S) counter, revised by rakisaionji
                Manipulator.WritePatch(0x0000000140257A8E, new byte[] { 0xF0 }); // CREDIT(S)
                Manipulator.WritePatch(0x0000000140257A4F, new byte[] { 0x1E }); // FREE PLAY
            }
        }
    }
}
