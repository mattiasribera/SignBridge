# SignBridge

Learn the American Sign Language (ASL) alphabet with your camera, and turn fingerspelled words into voice.
.NET 10 MAUI app for **Windows** and **Android**. All AI runs **on the device** with ONNX Runtime — no internet, no images leave the device.

## Features

- **Practice** — the app shows a letter, a drawing of the sign and how to form it. Sign it in front of the camera; it scores you in real time (score, streak, best streak).
- **Sign to voice** — spell a word letter by letter; the app shows the sign it understood (with its reference drawing) and reads the word aloud with the device's offline text-to-speech (Spanish voice when available).
- **What the AI sees** — a live skeleton of the 21 hand points the model detected.

## How it works

```
camera frame ──► palm detector ──► hand landmark model ──► 63 features ──► MLP classifier ──► letter + probability
                 (MediaPipe,        (MediaPipe, 21 points)  (wrist-relative,    (26 letters)
                  ONNX)                                       scale-invariant)
```

The classifier only sees the **shape of the hand**, never the background, clothing or skin color.
That is the key design decision — see *Model evaluation* below.

Architecture (hexagonal / ports and adapters):

| Project | Role |
|---|---|
| `src/SignBridge.Core` | Domain and recognition, no UI: `ISignRecognizer` (port), `LandmarkSignRecognizer` (adapter), `PracticeSession`, `WordBuilder`, `AslAlphabet`. |
| `src/SignBridge` | MAUI app: pages, view models (MVVM Toolkit), camera loop, text-to-speech. Depends on the port only; `RecognizerProvider` is the single place that picks the adapter. |
| `tests/SignBridge.Core.Tests` | xUnit tests, including parity tests of the C# pipeline against the Python reference outputs. |
| `tools/landmark_reference.py` | Regenerates the Python reference outputs used by the parity tests. |

## Run it

Requirements: .NET 10 SDK, Visual Studio 2026 (Insiders) with the **.NET MAUI** workload.

**Windows**
1. Open `SignBridge.slnx`.
2. Set `SignBridge` as the startup project, pick **Windows Machine**, press **F5**.
3. Allow camera access if Windows asks (Settings › Privacy › Camera › *Let desktop apps access your camera*).

**Android (USB debugging, no APK signing needed)**
1. On the phone: enable *Developer options* and *USB debugging*, connect it by USB and accept the prompt.
2. In Visual Studio pick the phone under **Android Local Devices**, press **F5**.
3. Requires Android 7.0 (API 24) or newer.

Command line:

```bash
dotnet test tests/SignBridge.Core.Tests
dotnet build src/SignBridge -f net10.0-windows10.0.19041.0
dotnet build src/SignBridge -f net10.0-android
```

## Model evaluation (why landmarks)

All candidates were tested **cross-dataset**: on people and backgrounds the model never saw during training.

| Approach | Unseen signers (ASL-HG P9–P10) | Real-room photos (ASL Letters) | Size |
|---|---|---|---|
| EfficientNet-B0 image classifier (first attempt) | 16.8% | 38–48% | 16 MB |
| SigLIP2 / ViT image classifiers | 44–48% | 45–52% | 330+ MB |
| **Hand landmarks + MLP (shipped)** | **88.7%** | **63–73%** | ~8 MB |

The first model scored 99% on its own dataset but failed on real webcams: its training data was a single signer
in front of a gray background, so it learned the scene instead of the hand. Lesson: **always evaluate on data from different people.**

Known limits of this pilot:
- ASL alphabet only (not LSB — Bolivian Sign Language). The recognizer is a port, so an LSB model can replace it without touching the UI.
- J and Z involve motion; a single-frame model cannot judge them, so Practice skips them.
- Accuracy on real webcams is still far from perfect; good light and the whole hand in view help.

## Credits and licenses

- Palm detection and hand landmark models: [OpenCV Zoo](https://huggingface.co/opencv) `palm_detection_mediapipe` and `handpose_estimation_mediapipe` (Apache-2.0), derived from Google MediaPipe.
- Landmark classifier trained on [ASL-HG](https://huggingface.co/datasets/juanjodurillo/asl-hg) (CC-BY-4.0) and [EitanG98/asl_letters](https://huggingface.co/datasets/EitanG98/asl_letters) (Unlicense). Test fixtures are photos from `EitanG98/asl_letters`.
- Sign drawings: Wikimedia Commons *Sign language A–Z* series (public domain).
