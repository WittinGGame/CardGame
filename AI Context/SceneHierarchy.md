1. Global Volume
2. Height Fog Global (3rd party add-on)
3. Lighting
4. Reflection Probe
5. The Visual Engine (3rd party add-on)
6. Main Camera
7. UI Camera
8. EventSystem
9. RunManager (RunManager.cs)
10. CardBattle_Test
  1. Systems
    1. 00_Run
      1. MainFlowController (MainFlowController.cs)
      2. BattleRunBridge (BattleRunBridge.cs)
      3. RunEndController (RunEndController.cs)
    2. 01_Map
      1. MapRuntimeController (MapRuntimeController.cs)
      2. TreeMapBattleFlowController (TreeMapBattleFlowController.cs)
    3. 02_Encounter
      1. RuntimeEncounterContext (RuntimeEncounterContext.cs)
      2. EncounterEnemySceneBinder (EncounterEnemySceneBinder.cs)
      3. EncounterCompletionController (EncounterCompletionController.cs)
      4. EncounterFlowResetController (EncounterFlowResetController.cs)
    4. 03_Battle
      1. BattleTestBootstrap (BattleTestBootstrap.cs)
      2. DeckController (DeckController.cs)
      3. CardResolver (CardResolver.cs)
      4. EnemyActionSystem (EnemyActionSystem.cs)
      5. BattleActionRunner (BattleActionRunner.cs)
      6. TargetSelectionSystem (TargetSelectionSystem.cs)
      7. BattleOutcomeController (BattleOutcomeController.cs)
      8. BattleEndPresentationController (BattleEndPresentationController.cs)
    5. 04_Reward
      1. RewardController (RewardController.cs)
    6. 05_Audio
      1. BattleAudio
        1. CardAudio (CardSFXController)
        2. CombatAudio (CombatSFXController.cs)
        3. UIAudio (UISFXController)
    7. 99_Debug
      1. EncounterDataDebugTest (EncounterDataDebugTest.cs)
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
11. UI_Canvas
  1. BattleUI
    1. HandUI
      1. HandPanel
    2. BattleHUD
      1. BG
      2. PlayerHp
      3. EndTurnButton
      4. DeckPlayer
      5. Graveyard
      6. PlayerAP
      7. Buff
    3. BattleVFX
      1. GraveyardVFXContainer
      2. GraveyardToDeckVFXController
      3. FloatingTextContainer
    4. TreeMapUI
      1. MapPanel
        1. LineContainer
        2. NodeContainer
        3. SelectedNodeInfoText
        4. StatusText
        5. StartBattleButton
    5. RewardUI
      1. RewardPanel
        1. DimBackground
        2. Title
        3. GoldRoot
          1. GoldAmountText
        4. ChoiceContainer
        5. NoChoicesRoot
        6. ResultText
        7. SkipButton
        8. ContinueButton
    6. RunEndUI
      1. RunCompletePanel
        1. Title
        2. SummaryText
        3. BackToMainButton
      2. RunFailedPanel
        1. Title
        2. SummaryText
        3. BackToMainButton
    7. MainFlowUI
      1. MainMenuPanel
        1. NewRunButton
        2. ContinueButton
        3. GameName
      2. CharacterSelectPanel
        1. KnightButton
        2. StartRunButton
        3. BackButton
        4. SelectedClassText
12. BattleFloatingTextSpawner
13. BattlePresentationController
14. RunPersistenceDebugTest

