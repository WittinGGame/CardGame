1. Global Volume
2. Height Fog Global (3rd party add-on)
3. Lighting
4. Reflection Probe
5. The Visual Engine (3rd party add-on)
6. Main Camera
7. UI Camera
8. EventSystem
9. CardBattle_Test
  1. Systems
    1. DeckController (DeckController.cs)
    2. CardResolver (CardResolver.cs)
    3. EnemyActionSystem (EnemyActionSystem.cs)
    4. BattleTestBootstrap (BattleTestBootstrap.cs)
    5. TargetSelectionSystem (TargetSelectionSystem.cs)
    6. BattleActionRunner (BattleActionRunner.cs)
    7. BattleAudio
      1. CardAudio (CardSFXController)
      2. CombatAudio (CombatSFXController.cs)
      3. UIAudio (UISFXController)
    8. BattleOutcomeController (BattleOutcomeController.cs)
    9. BattleEndPresentationController (BattleEndPresentationController.cs)
  2. Units
    1. Player (PlayerBattleUnit.cs)
    2. Enemy_01 (EnemyBattleUnit.cs)
      1. Model (Animator, BattleUnitView.cs)
      2. TargetCollider (Box Collider, TargetableEnemy.cs, EnemyTargetHighlight)
      3. UIAnchor_HP
      4. UIAnchor_Intent
      5. UIAnchor_Buff
      6. EnemyHighlight
      7. AttackAudio (CombatSFXController.cs)
    3. Enemy_02 (EnemyBattleUnit.cs)
  3. Environment
10. UI_Canvas
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
11. BattleFloatingTextSpawner
12. BattlePresentationController

