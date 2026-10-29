# USELESS AI — .NET desktop companion (expanded starter)

A Windows WPF starter; Node.js is not required. Designed around a black Dynamic-Island-like panel at the top-right, compact round mode, and a separate animated floating mascot.

## Included
- WPF desktop UI, round compact state and expanded rectangular panel.
- Floating mascot outside the panel; idle phrases, gentle float animation, eyes track cursor.
- Global `Ctrl+Shift+U` hotkey registration; `Esc` minimizes when the panel has focus.
- Speech output using Windows voices (`System.Speech.Synthesis`) and a greeting on startup.
- Basic speech recognition via Windows speech recognizer (`System.Speech.Recognition`); availability and language depend on installed Windows speech packs and microphone.
- Ollama chat integration (`/api/chat`) and model selector for coder/chat/vision model names.
- Screen-area selector overlay; capture selected region; sends image to a selected vision model such as `llava:7b`.
- Coding helper can open and read a selected source file and asks AI to review it. It does not overwrite files.
- Allowlisted app launch commands: Notepad, Calculator, Paint, File Explorer, VS Code.
- Simple settings for voice output.
- Asynchronous HTTP calls to keep the UI responsive.

## Requirements
- Windows 10/11, .NET 8 SDK, microphone if voice is used.
- Ollama installed and running for AI features.

## Run
Open the folder in VS Code with the C# extension, then in Terminal:
```powershell
dotnet restore
dotnet run
```
Install .NET 8 SDK from the official Microsoft .NET site if missing.

## Ollama models
For coding/chat:
```powershell
ollama pull qwen2.5-coder:3b
```
For image/screen analysis (larger model; may be slow on 8 GB RAM):
```powershell
ollama pull llava:7b
```
Ensure Ollama is running. The model selector chooses the requested model; each model must be downloaded first.

## Build a self-contained Windows x64 app
```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```
Output is under `bin\\Release\\net8.0-windows\\win-x64\\publish\\`.

## Honest feature audit / limitations
- This is an expanded starter, not a pixel-perfect recreation of the video.
- The speech greeting and answer readout use installed Windows speech voices. Voice recognition is dependent on Windows language/speech packs; Indonesian recognition may not be installed by default.
- Screen capture selects a rectangle and calls a vision model. Model memory requirements can be high on 8 GB RAM.
- Coding mode opens one selected file and asks the model to review it. It does not yet traverse an entire project, apply diffs, run tests, or automatically repair code.
- App launching uses a short allowlist and does not execute arbitrary shell commands.
- Mascot floats and tracks the cursor with its eyes; it does not yet freely walk around the entire desktop, do full petting gestures, or have a large library of expressive animations.
- Online/cloud AI provider integration, API key management, and automatic model failover are not included.
- The UI, speech APIs, global hotkey, capture overlay, and Ollama connection still need compilation and runtime testing on the target laptop. No claim of production readiness.
