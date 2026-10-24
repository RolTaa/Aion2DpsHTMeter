# 上流ソースについて

| 項目 | 内容 |
|---|---|
| リポジトリ | https://github.com/cyberbadger6969/aion2-dps-meter |
| 取り込んだコミット | `1a52dd94a30dc00c88805f70b2787a7a77100715`(v0.2.11, 2026-10-24) |
| ライセンス | GPL-3.0 |

## 取り込んだもの

- `src/AionMeter.Core/` … そのまま(下記の変更のみ)
- `src/AionMeter.Cli/` … そのまま
- `tests/AionMeter.Tests/` … そのまま(`HealingSnapshotTests.cs` は追加分)
- `data/` … そのまま
- `LICENSE`、`.gitignore` … そのまま
- `src/Aion2DpsHTMeter.App/app.manifest` … 上流の `AionMeter.App/app.manifest` の名前だけ変更

上流の `AionMeter.App`(WPF の UI 一式)は取り込んでいません。UI は `src/Aion2DpsHTMeter.App` で新規に作っています。

## 上流への変更

上流の更新(パッチ後の opcode 対応など)を取り込みやすくするため、変更は最小限にしています。

| ファイル | 変更 |
|---|---|
| `src/AionMeter.Core/Combat/Snapshots.cs` | `CombatantSnapshot` の末尾に `long Healing = 0` を追加 |
| `src/AionMeter.Core/Combat/CombatTracker.cs` | `BuildSnapshot` で `c.Healing` を渡す |
| `src/AionMeter.Core/Protocol/FrameDecoder.cs` | LZ4 展開先を `ArrayPool` の貸し借りから `new byte[]` に変更(読みやすさ優先) |
| `src/AionMeter.Core/Protocol/PacketParser.cs` | 同上 |
| `src/AionMeter.Core/Combat/CombatTracker.cs` | 回復の集計を修正: 味方への回復(プレイヤー→プレイヤーのダメージ記録)を回復として数える、回復だけのプレイヤーにも行を作る、MP回復と敵の自己回復は除外(A2Tools-DPS-Meter の読み方に合わせた) |
| `src/AionMeter.Core/Game/GameData.cs` | `resource_restore_skills.json`(MP等だけを回復するスキル)を読み込む |
| `data/resource_restore_skills.json` | 追加。A2Tools-DPS-Meter `src/data/resource_restore_skills.json`(commit `705287b`)を配列形式に変換 |

## 上流の更新を取り込む手順

1. 上流を clone し、新しいコミットとの差分を確認する
   (`git diff 1a52dd9..<新コミット> -- src/AionMeter.Core src/AionMeter.Cli tests data`)
2. 差分をこのリポジトリに当て、上の「上流への変更」が残っていることを確認する
3. `dotnet test` が通ることを確認し、このファイルのコミット番号を更新する
