# Aion2DpsHTMeter

AION2 のボス戦で、プレイヤーごとの **DPS・回復量・被ダメージ** をリアルタイムに表示するオーバーレイです。

- 左端にクラス色の縦棒(順位を表示)
- 横バーは上から DPS・回復量・被ダメージ、縦幅 3:1:1
- DPS バーの上にキャラ名、右端に値を DPS/合計ダメージ → 回復量 → 被ダメージの順で表示(すべて K 表記)
- 並び順はプルダウンで選択(DPS / 回復量 / 被ダメージ)
- ボスとの戦闘だけを表示(雑魚戦は表示しない。次のボス戦が始まるまで直前のボス戦を表示)
- 「PTメンバーのみ」で自分とパーティメンバーだけに絞り込み(パーティ名簿の通信から判定)

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
| × ボタン | 終了 |

設定は `%LocalAppData%\Aion2DpsHTMeter\settings.json` に保存されます。設定画面は未実装です。

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
