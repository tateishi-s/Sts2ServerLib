# Sts2ServerLib TODO

## Phase 1: ライブラリ骨格の作成
- [x] `server/` ディレクトリを作成
- [x] `server/Sts2ServerLib.csproj` を作成
- [x] `server/docs/` ディレクトリを作成
- [x] `server/docs/Sts2ServerLib-design.md` を作成
- [x] `server/docs/Sts2ServerLib-todo.md` を作成（本ファイル）
- [x] ビルド確認: `dotnet build server/Sts2ServerLib.csproj`

## Phase 2: StateServer.cs の移動と修正
- [x] `mod/StateServer.cs` を `server/StateServer.cs` にコピー
- [x] namespace を `DamageCalcMod` → `Sts2ServerLib` に変更
- [x] ログプレフィックスを `[DamageCalcMod]` → `[Sts2ServerLib]` に変更（3箇所）
  - [x] `Start()` メソッド（53行）
  - [x] `ListenLoop()` メソッド（74行）
  - [x] `HandleRequest()` メソッド（114行）
- [x] ビルド確認: `dotnet build server/Sts2ServerLib.csproj`

## Phase 3: DamageCalcMod の書き換え
- [x] `Directory.Build.props` に `Sts2ServerLibRoot` 自動検出ロジックを追加
- [x] `mod/DamageCalcMod.csproj` に `ProjectReference` を追加
- [x] `mod/ModEntry.cs` に `using Sts2ServerLib;` を追加
- [x] `mod/StateServer.cs` を削除
- [x] ビルド確認: `dotnet build mod/DamageCalcMod.csproj`

## Phase 4: テスト更新
- [x] `tests/DamageCalcMod.Tests.csproj` のソースリンクを `$(Sts2ServerLibRoot)StateServer.cs` に更新
- [x] `tests/StateServerTests.cs` の `using DamageCalcMod;` を `using Sts2ServerLib;` に変更
- [x] テスト実行: `cd tests && dotnet test`（全件パス確認）

## Phase 5: deploy.sh・ドキュメント・submodule 更新
- [x] `deploy.sh` に `Sts2ServerLib` ローカル検出ロジックを追加
- [x] `deploy.sh` に `Sts2ServerLib.dll` のコピーを追加
- [ ] GitHub リポジトリ `tateishi-s/Sts2ServerLib` を作成
- [ ] `server/` の内容を GitHub リポジトリにプッシュ
- [x] `.gitmodules` に `server/` submodule エントリを追加
- [x] `README.md` を更新（`server/` の追加）
- [x] `CLAUDE.md` を更新（アーキテクチャ説明・ビルドコマンド）
- [x] デプロイ確認: `bash deploy.sh`
- [x] ゲーム起動 → 戦闘 → `[Sts2ServerLib] HTTPサーバー起動:` ログ確認
- [x] `http://localhost:21345/state` で JSON 確認・Web UI 動作確認
