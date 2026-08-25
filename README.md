# 3D Raid Boss

以 Unity 製作的 3D 俯視角王戰 Demo，玩法參考 MMORPG 團隊副本，聚焦在三分鐘 Boss 時間軸、攻擊預告、走位判定、玩家技能循環與戰鬥紀錄。這個專案的目標不是製作完整 RPG，而是將王戰機制、UI 回饋及測試流程整理成一個可以實際操作的垂直切片。

## Demo 內容

- 三分鐘、三個階段的 Boss 戰鬥時間軸
- 4×4 場地格線及場外死亡判定
- 直線、棋盤、追蹤落雷、擊退、力場、連續爆炸、扇形掃掠與十字攻擊等機制
- 攻擊預告、讀條、傷害判定、擊退與安全區互動
- 包含讀條、GCD、疊層、充能、增益效果與冷卻時間的玩家技能系統
- HP、技能狀態、Boss 讀條、受傷訊息、戰鬥時間與 DPS 統計 HUD
- 勝利、失敗、暫停、設定、重新挑戰與返回主選單流程
- 每場戰鬥分開保存的 JSON 紀錄，可查看或刪除單筆紀錄

## 程式執行概念

```text
玩家輸入
  └─ PlayerController / SkillSystem
       ├─ 技能條件、GCD、傷害與疊層
       └─ HudManager 更新技能及傷害資訊

BossTimelineController
  └─ BossMechanicController
       ├─ 建立攻擊預告
       ├─ 延遲後進行命中、傷害與擊退判定
       └─ PlayerStats 記錄受傷來源與時間

GameManager
  └─ Playing / Paused / Dead / Victory
       └─ ResultScreen / GameRecordStore
```

Boss 的時間軸與地板機制分開處理：`BossTimelineController` 決定什麼時間觸發事件，`BossMechanicController` 負責預告視覺、命中與效果結算。重新開始戰鬥時，時間軸會停止執行中的 Coroutine、清除危險區並重設事件索引。

## 程式碼分類

| 路徑 | 職責 | 主要程式 |
|---|---|---|
| `Assets/Scripts/Core/` | 遊戲狀態、戰鬥計時、場景名稱、按鍵設定與紀錄保存 | `GameManager`、`BattleTimer`、`GameRecordStore` |
| `Assets/Scripts/Player/` | 玩家移動、HP、死亡、技能循環與技能提示特效 | `PlayerController`、`PlayerStats`、`SkillSystem` |
| `Assets/Scripts/Boss/` | 4×4 場地座標、三分鐘時間軸及各種 Boss 機制 | `BossArenaGrid`、`BossTimelineController`、`BossMechanicController` |
| `Assets/Scripts/UI/` | HUD、技能格、主選單、設定、暫停、結算與紀錄畫面 | `HudManager`、`SkillSlotUI`、`MainMenuController` |
| `Assets/Scripts/Arena/` | 戰鬥場地建立與設定 | `ArenaSetup` |
| `Assets/Scripts/Camera/` | 第三人稱俯視攝影機控制 | `CameraController` |
| `Assets/Scripts/Editor/` | 建立或修復場景、HUD、Boss 時間軸及危險區的 Editor 工具 | `SetupBossTimeline`、`RebuildHudNow` 等 |
| `Assets/Tests/Editor/` | 核心王戰機制的 EditMode 測試 | `BossSystemEditModeTests` |

## 重要程式進入點

- [`BossTimelineController.BuildEvents()`](Assets/Scripts/Boss/BossTimelineController.cs)：建立並排序三分鐘事件表，負責階段切換與最後的勝利判定。
- [`BossMechanicController`](Assets/Scripts/Boss/BossMechanicController.cs)：提供各類攻擊機制的公開方法，統一處理預告、結算、擊退與危險區清除。
- [`SkillSystem`](Assets/Scripts/Player/SkillSystem.cs)：處理玩家技能條件、GCD、冷卻、疊層、傷害統計與重設。
- [`HudManager`](Assets/Scripts/UI/HudManager.cs)：同步玩家／Boss 資訊、技能狀態、Boss 讀條及左上角戰鬥訊息。
- [`GameRecordStore`](Assets/Scripts/Core/GameRecord.cs)：將單場結果寫入 `Application.persistentDataPath/Records/`；每筆紀錄為獨立 JSON，最多保留 50 筆，並限制刪除路徑避免越界存取。

如果要快速閱讀專案，建議依序查看 `BossTimelineController` → `BossMechanicController` → `PlayerStats` → `HudManager`，可以看到一次 Boss 攻擊從時間軸觸發、顯示預告、造成傷害到 UI 留下紀錄的完整流程。

## 自動化測試

目前包含 25 項 EditMode 測試，範圍包括：

- 4×4 格子座標、行列 Bounds 與場外位置換算
- HP 扣除、死亡事件、立即死亡與場外判定
- 受傷來源、時間戳、JSON 保存及單筆刪除
- 紀錄刪除的路徑穿越防護
- 擊退減免、擊退方向、場地覆蓋半徑、扇形與力場判定
- 時間軸排序、指定時間開始、重設與三分鐘勝利流程

## 開啟方式

- Unity 版本：`6000.4.11f1`
- 從 Unity Hub 開啟此儲存庫根目錄。
- 主選單場景：`Assets/Scenes/MainMenu.unity`
- 戰鬥場景：`Assets/SampleScene.unity`

第一次開啟時，Unity 會解析並安裝 `Packages/manifest.json` 中列出的官方套件。測試可從 Unity Test Runner 的 EditMode 分頁執行。

## 公開版差異

原始私人專案使用了外部粒子特效包。為避免重新散布第三方素材，此公開版未包含該素材包及其範例，也移除了 Coplay、Unity MCP、Unity AI Assistant、本機快取、Unity Cloud 專案識別碼與私人開發歷史。核心玩法及攻擊預告仍可透過專案程式與 Unity 基礎圖形運作。

[`Docs/GameDesignDocument.txt`](Docs/GameDesignDocument.txt) 是專案早期的玩法規格，部分數值與內容已在後續實作中調整，請以目前程式與場景為準。

## 專案設計與 AI 協作

專案需求、王戰時間軸、技能規則、UI 行為、戰鬥紀錄需求、測試條件與迭代方向由專案製作者規劃；AI 輔助工具用於協助程式實作與除錯。

## 授權說明

此儲存庫僅開放原始碼供作品集審閱，除第三方授權聲明另有規定外，不授權複製、修改或重新散布專案程式。詳細內容請參考 [`LICENSE.md`](LICENSE.md) 與 [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md)。
