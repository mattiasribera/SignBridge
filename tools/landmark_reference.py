"""Generate reference outputs of the landmark pipeline for C# parity tests.

Runs the OpenCV Zoo MediaPipe palm detector + hand landmark models and the landmark MLP
on a few images and writes, per image, the palm score, 21 landmarks, the 63 features
and the MLP probabilities.

Usage:
    python tools/landmark_reference.py --ocvzoo <dir with mp_palmdet.py, mp_handpose.py and .onnx>
        --mlp <asl_landmark_mlp.onnx> --out tests/SignBridge.Core.Tests/Fixtures img1.jpg img2.jpg ...
"""

import argparse
import json
import pathlib
import sys

import cv2 as cv
import numpy as np
import onnxruntime as ort


def features(landmarks: np.ndarray) -> np.ndarray:
    """Wrist-relative, scaled by the 2D wrist->middle-finger-base length. No mirroring."""
    p = landmarks - landmarks[:1]
    scale = max(float(np.linalg.norm(p[9, :2])), 1e-6)
    return (p / scale).reshape(-1).astype(np.float32)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--ocvzoo", type=pathlib.Path, required=True)
    parser.add_argument("--mlp", type=pathlib.Path, required=True)
    parser.add_argument("--out", type=pathlib.Path, required=True)
    parser.add_argument("images", nargs="+", type=pathlib.Path)
    args = parser.parse_args()

    sys.path.insert(0, str(args.ocvzoo))
    from mp_handpose import MPHandPose
    from mp_palmdet import MPPalmDet

    palm_det = MPPalmDet(str(args.ocvzoo / "palm_detection_mediapipe_2023feb.onnx"), nmsThreshold=0.3, scoreThreshold=0.6)
    hand_pose = MPHandPose(str(args.ocvzoo / "handpose_estimation_mediapipe_2023feb.onnx"), confThreshold=0.8)
    mlp = ort.InferenceSession(str(args.mlp))

    reference = {"anchors": palm_det.anchors.astype(float).round(8).tolist(), "images": {}}
    for image_path in args.images:
        bgr = cv.imread(str(image_path))
        palms = palm_det.infer(bgr)
        entry = {"palm_score": None, "landmarks": None, "features": None, "probabilities": None}
        if len(palms):
            palm = palms[int(np.argmax(palms[:, -1]))]
            entry["palm_score"] = float(palm[-1])
            entry["palm_box"] = palm[0:4].astype(float).tolist()
            hand = hand_pose.infer(bgr, palm)
            if hand is not None:
                landmarks = hand[4:67].reshape(21, 3).astype(np.float32)
                feats = features(landmarks)
                probs = mlp.run(["probabilities"], {"X": feats[None, :]})[0][0]
                entry.update(
                    landmarks=landmarks.astype(float).tolist(),
                    features=feats.astype(float).tolist(),
                    probabilities=probs.astype(float).tolist(),
                )
        reference["images"][image_path.name] = entry
        print(image_path.name, entry["palm_score"], None if entry["probabilities"] is None else int(np.argmax(entry["probabilities"])))

    (args.out / "landmark_reference.json").write_text(json.dumps(reference))


if __name__ == "__main__":
    main()
