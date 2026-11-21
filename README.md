# Aion2DpsHTMeter

AION2 のボス戦で、プレイヤーごとの **DPS・回復量・被ダメージ** をリアルタイムに表示するオーバーレイです。

- 幅300pxの小さなパネル。左端にクラス色の縦棒(順位を表示)
- 横バーは上から 累計ダメージ・回復量・被ダメージ、縦幅 16/2/2px。名前と値は総ダメージのバーに重ねて表示
- 値は DPS/合計ダメージ・回復量・被ダメージを1行で表示。すべて K 表記で、合計ダメージのみ小数なし
- ダメージは、戦闘で攻撃した NPC のうち最大HPの相手(ボスがいればボス)への分を表示。DPS は直近1分間の平均
- ヘッダーの名前・HP・撃破予測も、その最大HPの相手のもの
- 時計はボスの撃破予測時間(直近20秒のHPの減り方から計算)。撃破後・終了後は戦闘時間
- 並び順はプルダウンで選択(DPS〈初期値〉/ 総ダメージ / 総回復 / 総被ダメージ)
- 表示する人はボタンで 周囲(戦闘に参加している全員)/ 個人(自分のみ)を切り替え
- 計測対象は設定画面で選択(アビスのアーティファクト / ボス / 雑魚)。選んだ種類の戦闘だけを表示し、他の戦闘中は直前の結果を表示

通信の取得と解析は [cyberbadger6969/aion2-dps-meter](https://github.com/cyberbadger6969/aion2-dps-meter) の
`AionMeter.Core` を流用しています(詳細は [UPSTREAM.md](UPSTREAM.md))。

## 必要なもの

- Windows 10 / 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)(ビルドする場合)
- [Npcap](https://npcap.com/)(インストール時に「WinPcap API-compatible Mode」をオン)

## ビルドと実行

```powershell
dotnet build -c Release
dotnet run --project src/Aion2DpsHTMeter.App -c Release            # 実際の通信を読む
dotnet run --project src/Aion2DpsHTMeter.App -c Release -- --demo  # ゲームなしでデモ戦闘を表示
dotnet test -c Release
```

配布用の exe を作る場合:

```powershell
dotnet publish src/Aion2DpsHTMeter.App -c Release -r win-x64 --self-contained false -o dist
```

WPF プロジェクトは `EnableWindowsTargeting` を有効にしているので、Linux / macOS でもビルドとテストは通ります
(実行は Windows のみ)。

## 操作

| 操作 | 内容 |
|---|---|
| ヘッダーをドラッグ | 位置の移動(位置は保存される) |
| 鍵ボタン / Ctrl+Shift+L | クリックスルーの切り替え。オン中はボタンが押せないのでショートカットで戻す |
| 矢印ボタン | 計測リセット |
| 歯車ボタン | 設定(計測対象、背景の不透明度) |
| 「周囲 / 個人」ボタン | 表示する人の切り替え |
| × ボタン | 終了 |

設定は `%LocalAppData%\Aion2DpsHTMeter\settings.json` に保存されます。

アーティファクト戦の対象 NPC は `data/abyss_artifact_npcs.json` に NPC コードで書いてあります。今は仮に
アビスの守護神将を入れているので、実際の対象が分かったら追加・削除してください。


## 構成

```
src/
  AionMeter.Core/        通信の取得・解読・戦闘集計(上流から流用)
  AionMeter.Cli/         記録した pcap の再生など開発用ツール(上流から流用)
  Aion2DpsHTMeter.App/   オーバーレイ本体(WPF、新規)
    Services/            MeterService(取得→集計の配線)、AppSettings、ClickThrough
    ViewModels/          OverlayViewModel、PlayerRowViewModel、ClassPalette
tests/AionMeter.Tests/   上流のテスト + 追加分(HealingSnapshotTests)
data/                    スキル・NPC などのゲームデータ表(上流から流用)
```

## ライセンス

GPL-3.0(上流が GPL-3.0 のため)。ゲームデータの出典は [data/NOTICE.md](data/NOTICE.md) を参照してください。
NCSOFT とは無関係の非公式ツールです。
