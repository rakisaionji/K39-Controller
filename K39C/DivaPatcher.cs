using System.Collections.Generic;

namespace K39C
{
    internal class DivaPatcher
    {
        Settings Settings;
        Manipulator Manipulator;
        Dictionary<long, byte[]> patches;

        private const int SYS_TIMER_TIME = 60 * 39;
        // private const long SEL_PV_TIME_ADDRESS = 0x000000014CC12498L;

        internal DivaPatcher(Manipulator manipulator, Settings settings)
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

        internal void ApplyPatches()
        {
            foreach (var patch in patches)
                Manipulator.WritePatch(patch.Key, patch.Value);
            if (Settings.DivaPatches.RamPathFix)
                // Write ram files to the current directory instead of Y:/SBZV/ram
                Manipulator.WritePatch(0x000000014066CF09, new byte[] { 0xE9, 0xD8, 0x00 });
            if (Settings.DivaPatches.FreePlay)
            {
                // Always return true for the SelCredit enter SelPv check
                Manipulator.WritePatch(0x0000000140393610, new byte[] { 0xB0, 0x01, 0xC3, 0x90, 0x90, 0x90 });
                Manipulator.WritePatch(0x000000014066E870, new byte[] { 0xEB, 0x0E }); // Thanks vladkorotnev
            }
            if (Settings.DivaPatches.GlutCursor != GlutCursor.NONE)
                // Use GLUT_CURSOR_RIGHT_ARROW instead of GLUT_CURSOR_NONE
                Manipulator.WritePatch(0x000000014019341B, new byte[] { (byte)Settings.DivaPatches.GlutCursor });
            if (Settings.DivaPatches.HideCredits)
            {
                // Dirty hide of CREDIT(S) counter by rakisaionji
                Manipulator.WritePatch(0x00000001409F6200, new byte[] { 0x00 }); // CREDIT(S)
                Manipulator.WritePatch(0x00000001409F61F0, new byte[] { 0x00 }); // FREE PLAY
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
            Manipulator.WritePatch(0x0000000140A3E720, new byte[] { 0x00 });
            Manipulator.WritePatch(0x0000000140A3E7C8, new byte[] { 0x00 });
            if (Settings.DivaPatches.MdataPathFix)
            {
                // Change mdata path from "C:/Mount/Option" to "mdata", revised by rakisaionji
                Manipulator.WritePatch(0x000000014066CE9C, new byte[] { 0x05 }); // Size
                Manipulator.WritePatch(0x000000014066CEA3, new byte[] { 0xF1, 0x1D, 0x39 });
                Manipulator.WritePatch(0x000000014066CEAE, new byte[] { 0x05 }); // Size
            }
            // Touch effect is annoying without Scale Component in other resolutions
            if (Settings.Executable.IsCustomRes() && !Settings.Components.ScaleComponent)
            {
                Manipulator.WritePatch(0x00000001406A1FE2, new byte[] { 0x7E });                                            // MOVQ  XMM0,qword ptr [0x168 + RSP] (change to MOVQ)
                Manipulator.WritePatch(0x00000001406A1FE9, new byte[] { 0x66, 0x0F, 0xD6, 0x44, 0x24, 0x6C });              // MOVQ  qword ptr [RSP + 0x6c],XMM0
                Manipulator.WritePatch(0x00000001406A1FEF, new byte[] { 0xC7, 0x44, 0x24, 0x74, 0x00, 0x00, 0x00, 0x00 });  // MOV  dword ptr [RSP + 0x74],0x0
                Manipulator.WritePatch(0x00000001406A1FF7, new byte[] { 0xEB, 0x0E });                                      // JMP  0x1406a2007 (to rest of function as usual)
                Manipulator.WritePatch(0x00000001406A1FF9, new byte[] { 0x66, 0x48, 0x0F, 0x6E, 0xC2 });              // MOVQ  XMM0,RDX (load touch pos)
                Manipulator.WritePatch(0x00000001406A1FFE, new byte[] { 0xEB, 0x5D });                                // JMP  0x1406a205d
                Manipulator.WritePatch(0x00000001406A205D, new byte[] { 0x0F, 0x2A, 0x0D, 0xB8, 0x6A, 0x31, 0x00 });  // CVTPI2PS  XMM1,qword ptr [0x1409b8b1c] (load 1280x720)
                Manipulator.WritePatch(0x00000001406A2064, new byte[] { 0x0F, 0x12, 0x51, 0x1C });                    // MOVLPS  XMM2,qword ptr [RCX + 0x1c] (load actual res)
                Manipulator.WritePatch(0x00000001406A2068, new byte[] { 0xE9, 0x14, 0xFF, 0xFF, 0xFF });              // JMP  0x1406a1f81
                Manipulator.WritePatch(0x00000001406A1F81, new byte[] { 0x0F, 0x59, 0xC1 });                          // MULPS  XMM0,XMM1
                Manipulator.WritePatch(0x00000001406A1F84, new byte[] { 0x0F, 0x5E, 0xC2 });                          // DIVPS  XMM0,XMM2
                Manipulator.WritePatch(0x00000001406A1F87, new byte[] { 0x66, 0x0F, 0xD6, 0x44, 0x24, 0x10 });        // MOVQ  qword ptr [RSP+0x10],XMM0
                Manipulator.WritePatch(0x00000001406A1F8D, new byte[] { 0xEB, 0x06 });                                // JMP  0x1406a1f95 (back to original function)
                Manipulator.WritePatch(0x00000001406A1F90, new byte[] { 0xEB, 0x67 });                                // JMP  0x1406a1ff9
            }
            // Skip Error Display in ADVERTISE by rakisaionji
            switch (Settings.System.ErrorDisplay)
            {
                case ErrorDisplay.SKIP_CARD:
                    Manipulator.WritePatch(0x00000001403BA7E7, new byte[] { 0xEB, 0x46 });
                    Manipulator.WritePatch(0x00000001403BA909, new byte[] { 0xEB, 0x1F });
                    break;
                case ErrorDisplay.HIDDEN:
                    Manipulator.WritePatch(0x00000001403BA7E7, new byte[] { 0xE9, 0x3C, 0x03, 0x00, 0x00 });
                    break;
                case ErrorDisplay.DEFAULT:
                    break;
            }
            // Force Hide Main ID and Keychip ID
            if (Settings.System.HideId)
            {
                Manipulator.WritePatch(0x00000001409A5918, new byte[] { 0x00 });
                Manipulator.WritePatch(0x00000001409A5928, new byte[] { 0x00 });
            }
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
                Manipulator.WritePatch(0x000000014048FA91, new byte[] { 0xEB, 0x6F }); // skip button panel image ( JMP  0x14048FB02 )

                // patch minimum PV UI state to 1 instead of 0
                // hook check for lyrics enabled (UI state < 2) to change UI state 0 into 1
                // dump new code in the skipped button panel condition
                Manipulator.WritePatch(0x000000014048FA93, new byte[] { 0xC7, 0x83, 0x58, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 }); // MOV  dword ptr [0x158 + RBX],0x1
                Manipulator.WritePatch(0x000000014048FA9D, new byte[] { 0xC6, 0x80, 0x3A, 0xD1, 0x02, 0x00, 0x01 }); // MOV  byte ptr [0x2d13a + RAX],0x1
                Manipulator.WritePatch(0x000000014048FAA4, new byte[] { 0xE9, 0x8B, 0xFB, 0xFF, 0xFF }); // JMP  0x14048F634

                Manipulator.WritePatch(0x000000014048F62D, new byte[] { 0xE9, 0x61, 0x04, 0x00, 0x00 }); // JMP  0x14048FA93
            }
            if (Settings.DivaPatches.HideLyrics)
            {
                Manipulator.WritePatch(0x00000001404E7A25, new byte[] { 0x00, 0x00 });
                Manipulator.WritePatch(0x00000001404E7950, new byte[] { 0x48, 0xE9 }); // ensure first iteration doesn't run
            }
            if (Settings.DivaPatches.HidePvMark)
                Manipulator.WritePatch(0x0000000140A13A88, new byte[] { 0x00 });
            if (Settings.DivaPatches.HideSeBtn)
                Manipulator.WritePatch(0x00000001409A4D60, new byte[] { 0xC0, 0xD3 });
            if (Settings.DivaPatches.HideVolume)
                Manipulator.WritePatch(0x0000000140A85F10, new byte[] { 0xE0, 0x50 });
            // System Timer Patches, by samyuu and rakisaionji
            var sys_timer = (int)Settings.System.SysTimer;
            if (sys_timer > 1) // HIDDEN
            {
                Manipulator.WritePatch(0x00000001409C0758, new byte[] { 0x00 });
                Manipulator.WritePatch(0x0000000140A3D3F0, new byte[] { 0x00 });
                Manipulator.WritePatch(0x0000000140A3D3F8, new byte[] { 0x00 });
            }
            if (sys_timer > 0) // FREEZE
            {
                // SEL_CARD_TIMER
                Manipulator.WriteInt32(0x0000000141802660, SYS_TIMER_TIME);
                Manipulator.WritePatchNop(0x0000000140566B9E, 3);
                Manipulator.WritePatchNop(0x0000000140566AEF, 3);
                // SEL_PV_TIMER
                Manipulator.WriteInt32(0x00000001405C514A, SYS_TIMER_TIME);
                Manipulator.WritePatchNop(0x00000001405BDFBF, 6);
                Manipulator.WritePatchNop(0x00000001405C517A, 6);
            }
            // Anti-alias Patches, by lybxlpsv and nastys
            if (!Settings.System.TemporalAA) // Disable Temporal AA
            {
                // Set TAA var (shouldn't be needed but whatever)
                Manipulator.WriteByte(0x00000001411AB67C, 0);
                // Make constructor/init not set TAA
                Manipulator.WritePatchNop(0x00000001404AB11D, 3);
                // Not sure, but it's somewhere in TaskPvGame init
                // Just make it set TAA to 0 instead of 1 to avoid possible issues
                Manipulator.WritePatch(0x00000001401063CE, new byte[] { 0x00 });
                // Prevent re-enabling after taking photos
                Manipulator.WritePatch(0x000000014048FBA9, new byte[] { 0x00 });
            }
            if (!Settings.System.MorphologicalAA)
            {
                // Set MLAA var (shouldn't be needed but whatever)
                Manipulator.WriteByte(0x00000001411AB680, 0);
                // Make constructor/init not set MLAA
                Manipulator.WritePatchNop(0x00000001404AB11A, 3);
            }
        }
    }
}
