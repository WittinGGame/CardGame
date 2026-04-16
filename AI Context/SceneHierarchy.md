1. Main Camera
2. Global Volume
3. EventSystem
4. CardBattle_Test
    1. Systems
        1. DeckController (DeckController.cs)
        2. CardResolver (CardResolver.cs)
        3. EnemyActionSystem (EnemyActionSystem.cs)
        4. BattleTestBootstrap (BattleTestBootstrap.cs)
        5. TargetSelectionSystem (TargetSelectionSystem.cs)
        6. BattleActionRunner (BattleActionRunner.cs)
    2. Units
        1. Player (PlayerBattleUnit.cs)
        2. Enemy_01 (EnemyBattleUnit.cs)
            1. Model (Animator, BattleUnitView.cs)
            2. TargetCollider (Box Collider, TargetableEnemy.cs, EnemyTargetHighlight)
            3. UIAnchor_HP
            4. UIAnchor_Intent
            5. UIAnchor_Buff
            6. EnemyHighlight
        3. Enemy_02 (EnemyBattleUnit.cs)
    3. Environment
5. UI_Canvas
    1. BG
    2. HandUI
        1. HandPanel
    3. BattleHUD
        1. PlayerHp
        2. EndTurnButton
        3. DeckPlayer
        4. Graveyard
        5. PlayerAP
    4. EnemyUIContainer