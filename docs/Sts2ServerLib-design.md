# Sts2ServerLib 設計判断記録

## 概要

`Sts2ServerLib` は Slay the Spire 2 MOD 向けの汎用 HTTP サーバーライブラリ。
Godot・STS2 DLL に一切依存しない純粋な .NET 実装のため、複数の MOD で再利用可能。

もとは `DamageCalcMod` の `StateServer.cs` として実装されていたが、
他 MOD でも HTTP API 機能を再利用できるよう独立ライブラリとして切り出した。

## 設計判断

### 依存関係を持たない設計

- Godot (`GodotSharp.dll`) への依存なし → テストが Godot 不要で実行可能
- STS2 ゲーム DLL への依存なし → 汎用ライブラリとして再利用可能
- ロギング処理は `Action<string>` の DI 注入で外部から差し替え可能

### ファイル構成

```
server/
  Sts2ServerLib.csproj   — プロジェクトファイル
  StateServer.cs         — HTTP サーバー本体
  docs/
    Sts2ServerLib-design.md  — 本ファイル
    Sts2ServerLib-todo.md    — 実装 TODO チェックリスト
```

### ローカル開発 / submodule の切り替え

`Directory.Build.props` の `Sts2ServerLibRoot` プロパティで制御:
- `../Sts2ServerLib/Sts2ServerLib.csproj` が存在 → sibling ローカルリポジトリを優先
- 存在しない場合 → `server/` submodule を使用

### セキュリティ

- パストラバーサル対策: `webRoot` 外へのアクセスを 403 で拒否
- CORS ヘッダー付与 (`Access-Control-Allow-Origin: *`) で Web UI からの fetch を許可
- OPTIONS プリフライトに 204 で応答
