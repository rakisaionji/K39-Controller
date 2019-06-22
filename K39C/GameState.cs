﻿using System;

namespace K39C
{
    public enum GameState : UInt32
    {
        GS_STARTUP,
        GS_ADVERTISE,
        GS_GAME,
        GS_DATA_TEST,
        GS_TEST_MODE,
        GS_APP_ERROR,
        GS_MAX,
    }

    public enum SubGameState : UInt32
    {
        SUB_DATA_INITIALIZE,
        SUB_SYSTEM_STARTUP,
        SUB_SYSTEM_STARTUP_ERROR,
        SUB_WARNING,
        SUB_LOGO,
        SUB_RATING,
        SUB_DEMO,
        SUB_TITLE,
        SUB_RANKING,
        SUB_SCORE_RANKING,
        SUB_CM,
        SUB_SELECTOR,
        SUB_GAME_MAIN,
        SUB_GAME_SEL,
        SUB_STAGE_RESULT,
        SUB_SCREEN_SHOT_SEL,
        SUB_GAME_OVER,
        SUB_DATA_TEST_MAIN,
        SUB_DATA_TEST_MISC,
        SUB_DATA_TEST_OBJ,
        SUB_DATA_TEST_STG,
        SUB_DATA_TEST_MOT,
        SUB_DATA_TEST_COLLISION,
        SUB_DATA_TEST_SPR,
        SUB_DATA_TEST_AET,
        SUB_DATA_TEST_AUTH_3D,
        SUB_DATA_TEST_CHR,
        SUB_DATA_TEST_ITEM,
        SUB_DATA_TEST_PERF,
        SUB_DATA_TEST_PVSCRIPT,
        SUB_DATA_TEST_PRINT,
        SUB_DATA_TEST_CARD,
        SUB_DATA_TEST_OPD,
        SUB_DATA_TEST_SLIDER,
        SUB_TEST_MODE_MAIN,
        SUB_APP_ERROR,
        SUB_MAX,
    }
}
