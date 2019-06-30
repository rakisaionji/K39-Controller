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
            if (Settings.DivaPatches.FreePlay)
                // Always return true for the SelCredit enter SelPv check
                Manipulator.WritePatch(0x0000000140393610, new byte[] { 0xB0, 0x01, 0xC3, 0x90, 0x90, 0x90 });
            if (Settings.DivaPatches.GlutCursor != GlutCursor.NONE)
                // Use GLUT_CURSOR_RIGHT_ARROW instead of GLUT_CURSOR_NONE
                Manipulator.WritePatch(0x000000014019341B, new byte[] { (byte)Settings.DivaPatches.GlutCursor });
            if (Settings.DivaPatches.HideCredits)
            {
                // Dirty hide of CREDIT(S) counter, revised by rakisaionji
                Manipulator.WritePatch(0x00000001403BAC2E, new byte[] { 0xD8 }); // CREDIT(S)
                Manipulator.WritePatch(0x00000001403BABEF, new byte[] { 0x06, 0xB6 }); // FREE PLAY
            }
            if (Settings.Components.PlayerDataManager)
            {
                // Shitty way to initialize Level Name by rakisaionji
                Manipulator.WritePatch(0x0000000140205143, new byte[] {
                    0xE8, 0xC8, 0x27, 0xE0, 0xFF, 0x48, 0x8D, 0x8F, 0x00, 0x01, 0x00, 0x00, 0x44, 0x8D, 0x45, 0x1E,
                    0x48, 0x8D, 0x15, 0x5E, 0x60, 0x7D, 0x00, 0xE8, 0xB1, 0x27, 0xE0, 0xFF });
                Manipulator.WritePatchNop(0x000000014020515F, 20);
            }
            // Hide Data Loading text when use_card = 1 by rakisaionji
            Manipulator.WritePatch(0x00000001405BA42D, new byte[] { 0xF7 });
            Manipulator.WritePatch(0x00000001405BB95C, new byte[] { 0x6F });
            // Change mdata path from "C:/Mount/Option" to "mdata", revised by rakisaionji
            Manipulator.WritePatch(0x000000014066CE9C, new byte[] { 0x05 }); // Size
            Manipulator.WritePatch(0x000000014066CEA3, new byte[] { 0xF1, 0x1D, 0x39 });
            Manipulator.WritePatch(0x000000014066CEAE, new byte[] { 0x05 }); // Size
            // Touch effect is annoying without Scale Component in other resolutions
            // It's not cool, just yeet it for fuck's sake, by rakisaionji
            if (Settings.Executable.IsCustomRes() && !Settings.Components.ScaleComponent)
                Manipulator.WritePatch(0x00000001406A1FD7, new byte[] { 0xEE });
            // Other Features by somewhatlurker, improved by rakisaionji
            var cardStatus = Settings.DivaPatches.CardIcon;
            if (cardStatus != StatusIcon.DEFAULT)
            {
                byte[] cardIcon;
                switch (cardStatus)
                {
                    case StatusIcon.ERROR:
                        cardIcon = new byte[] { 0xFA, 0x0A };
                        break;
                    case StatusIcon.WARNING:
                        cardIcon = new byte[] { 0xFB, 0x0A };
                        break;
                    case StatusIcon.OK:
                        cardIcon = new byte[] { 0xFC, 0x0A };
                        break;
                    default:
                        cardIcon = new byte[] { 0xFD, 0x0A };
                        break;
                }
                Manipulator.WritePatch(0x00000001403B9D6E, cardIcon); // error state
                Manipulator.WritePatch(0x00000001403B9D73, cardIcon); // ok state
            }
            var netStatus = Settings.DivaPatches.NetIcon;
            if (netStatus != StatusIcon.DEFAULT)
            {
                byte[] netIcon;
                switch (netStatus)
                {
                    case StatusIcon.ERROR:
                        netIcon = new byte[] { 0x9F, 0x1E };
                        break;
                    case StatusIcon.WARNING:
                        netIcon = new byte[] { 0xA1, 0x1E };
                        break;
                    case StatusIcon.OK:
                        netIcon = new byte[] { 0xA0, 0x1E };
                        break;
                    default:
                        netIcon = new byte[] { 0x9E, 0x1E };
                        break;
                }
                // network icon
                Manipulator.WritePatch(0x00000001403BA14B, netIcon); // error state
                Manipulator.WritePatch(0x00000001403BA155, netIcon); // ok state
                Manipulator.WritePatch(0x00000001403BA16B, netIcon); // partial state
                // never show the error code for partial connection
                Manipulator.WritePatch(0x00000001403BA1A5, new byte[] { 0x48, 0xE9 }); // jle --> jmp
            }
            if (Settings.DivaPatches.HidePvUi)
            {
                Manipulator.WritePatch(0x000000014048F594, new byte[] { 0x6A, 0x05 }); // skip button panel image
                // Manipulator.WritePatch(0x000000014048F59C, new byte[] { 0x77, 0x04 }); // skip screenshot stuff (actually seems not relevant)

                // patch minimum PV UI state to 1 instead of 0
                // hook check for lyrics enabled (UI state < 2) to change UI state 0 into 1
                // dump new code in the skipped button panel condition
                Manipulator.WritePatch(0x000000014048FA26, new byte[] { 0xC7, 0x83, 0x58, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 });  // MOV dword ptr [0x158 + RBX], 0x1
                Manipulator.WritePatch(0x000000014048FA30, new byte[] { 0xC6, 0x80, 0x3A, 0xD1, 0x02, 0x00, 0x01 });                    // MOV byte ptr [0x2d13a + RAX], 0x1
                Manipulator.WritePatch(0x000000014048FA37, new byte[] { 0xE9, 0xF8, 0xFB, 0xFF, 0xFF });                                // JMP 0x14048f634

                Manipulator.WritePatch(0x000000014048F62D, new byte[] { 0xE9, 0xF4, 0x03, 0x00, 0x00 }); // JMP 0x14048FA26
            }
            if (Settings.DivaPatches.HideLyrics)
            {
                Manipulator.WritePatch(0x00000001404E7A25, new byte[] { 0x00, 0x00 });
                Manipulator.WritePatch(0x00000001404E7950, new byte[] { 0x48, 0xE9 }); // ensure first iteration doesn't run
            }
            if (Settings.DivaPatches.HidePvMark) // Revised by rakisaionji
                Manipulator.WritePatch(0x000000014048FA59, new byte[] { 0x32 });
            if (Settings.DivaPatches.HideSeBtn) // Revised by rakisaionji
                Manipulator.WritePatchNop(0x000000014013CE58, 19);
            if (Settings.DivaPatches.HideVolume) // Revised by rakisaionji
                Manipulator.WritePatchNop(0x0000000140624BDF, 15);
        }
    }
}
