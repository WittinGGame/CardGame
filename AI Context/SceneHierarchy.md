1. Scene Hierachy,
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
        1. ActiveRunSaveService (ActiveRunSaveService.cs)
        2. ActiveRunAutoSaveController (ActiveRunAutoSaveController.cs)
        3. MainFlowController (MainFlowController.cs)
        4. BattleRunBridge (BattleRunBridge.cs)
        5. RunEndController (RunEndController.cs)
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
        2. MapActAuthoringDebugReporter (MapActAuthoringDebugReporter.cs)
        3. StatusDebugTest (StatusDebugTest.cs)
    2. Units
      1. Player (PlayerBattleUnit.cs)(StatusController.cs)
      2. Enemy_01 (EnemyBattleUnit.cs)(StatusController.cs)
        1. Model (Animator, BattleUnitView.cs)
        2. TargetCollider (Box Collider, TargetableEnemy.cs, EnemyTargetHighlight)
        3. UIAnchor_HP
        4. UIAnchor_Intent
        5. UIAnchor_Buff
        6. EnemyHighlight
        7. AttackAudio (CombatSFXController.cs)
      3. Enemy_02 (EnemyBattleUnit.cs)(StatusController.cs)
        1. Model (Animator, BattleUnitView.cs)
        2. TargetCollider (Box Collider, TargetableEnemy.cs, EnemyTargetHighlight)
        3. UIAnchor_HP
        4. UIAnchor_Intent
        5. UIAnchor_Buff
        6. EnemyHighlight
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
        7. PlayerStatusIconPanel
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
      5. RunTopBarController
        1. RunTopBarUI
          1. GoldText
            1. GoldLabelText
            2. GoldCountText
          2. DeckButton
            1. DeckLabelText
            2. DeckCountText
          3. RunTopBarController
            1. RunDeckPanel
              1. DimBackground
              2. Panel
                1. TitleText
                2. ScrollView
                  1. Viewport
                    1. Content
                3. EmptyText
                4. CloseButton
      6. RewardUI
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
      7. RunEndUI
        1. RunCompletePanel
          1. Title
          2. SummaryText
          3. BackToMainButton
        2. RunFailedPanel
          1. Title
          2. SummaryText
          3. BackToMainButton
      8. MainFlowUI
        1. MainMenuPanel
          1. NewRunButton
          2. ContinueButton
          3. GameName
        2. CharacterSelectPanel
          1. KnightButton
          2. StartRunButton
          3. BackButton
          4. SelectedClassText
        3. NewGameConfirmPanel
          1. Des
          2. Cancel
          3. Confirm
  12. BattleFloatingTextSpawner
  13. BattlePresentationController
  14. RunPersistenceDebugTest
2. EnemyUI.Prefab (EnemyUIController.cs)
  1. EnemyIntentUI (EnemyIntentUI.cs, WorldToUIFollow.cs)
  2. EnemyHpUI (EnemyStatusUI.cs, WorldToUIFollow.cs)
    1. EnemyHpBar
    2. EnemyName
    3. EnemyHpText
    4. EnemyAction
    5. EnemyActionPoint
    6. EnemyStatusIconPanel
      1. SlotContainer
    7. EnemyBlock
      1. EnemyBlockIcon
      2. EnemyBlockValue
  3. EnemyBuffUI (WorldToUIFollow.cs)
  4. EnemyBlock
    1. EnemyBlockIcon
    2. EnemyBlockValue
3. TreeMapNodeButtonPrefab
  1. MapNodeRing
  2. MapNodeGlow
  3. MapNodeBG
  4. MapNodeIcon
  5. MapNodeMarker
  6. MapNodeXMark

