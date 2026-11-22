"""Export the Hugging Face ASL alphabet classifier to ONNX and validate it.

Usage:
    python tools/export_onnx.py --out src/SignBridge/Resources/Raw

Produces:
    asl_efficientnet_b0.onnx  - model with input "pixel_values" [1,3,224,224], output "logits" [1,26]
    asl_labels.json           - class index -> letter
"""

import argparse
import json
import pathlib

import numpy as np
import onnxruntime as ort
import torch
from transformers import AutoImageProcessor, AutoModelForImageClassification

MODEL_ID = "abdollahhh/asl-sign-language-efficientnet-b0"
DATASET_ID = "Marxulia/asl_sign_languages_alphabets_v03"


def export(out_dir: pathlib.Path) -> pathlib.Path:
    model = AutoModelForImageClassification.from_pretrained(MODEL_ID).eval()
    onnx_path = out_dir / "asl_efficientnet_b0.onnx"
    dummy = torch.randn(1, 3, 224, 224)
    torch.onnx.export(
        model,
        (dummy,),
        str(onnx_path),
        input_names=["pixel_values"],
        output_names=["logits"],
        opset_version=17,
        dynamo=False,
    )
    labels = [model.config.id2label[i] for i in range(model.config.num_labels)]
    (out_dir / "asl_labels.json").write_text(json.dumps(labels))

    session = ort.InferenceSession(str(onnx_path), providers=["CPUExecutionProvider"])
    with torch.no_grad():
        expected = model(pixel_values=dummy).logits.numpy()
    actual = session.run(["logits"], {"pixel_values": dummy.numpy()})[0]
    print(f"max |torch - onnx| = {np.abs(expected - actual).max():.6f}")
    return onnx_path


def preprocess(image, processor) -> np.ndarray:
    """Mirror of the C# preprocessing: resize 224x224, scale /255, normalize."""
    image = image.convert("RGB").resize((224, 224))
    pixels = np.asarray(image, dtype=np.float32) / 255.0
    mean = np.array(processor.image_mean, dtype=np.float32)
    std = np.array(processor.image_std, dtype=np.float32)
    pixels = (pixels - mean) / std
    return pixels.transpose(2, 0, 1)[None, ...]


def validate(onnx_path: pathlib.Path, samples: int) -> None:
    from datasets import load_dataset

    processor = AutoImageProcessor.from_pretrained(MODEL_ID)
    labels = json.loads((onnx_path.parent / "asl_labels.json").read_text())
    session = ort.InferenceSession(str(onnx_path), providers=["CPUExecutionProvider"])
    # The dataset only publishes a train split, so this checks preprocessing parity,
    # not generalization to real camera frames.
    split = load_dataset(DATASET_ID, split="train")
    split = split.shuffle(seed=0).select(range(min(samples, len(split))))
    label_names = split.features["label"].names
    hits = 0
    for row in split:
        logits = session.run(["logits"], {"pixel_values": preprocess(row["image"], processor)})[0]
        hits += labels[int(logits.argmax())] == label_names[row["label"]]
    print(f"accuracy on {len(split)} dataset samples (manual preprocessing): {hits / len(split):.3f}")
    print(f"mean={processor.image_mean} std={processor.image_std}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", type=pathlib.Path, required=True)
    parser.add_argument("--samples", type=int, default=200)
    args = parser.parse_args()
    args.out.mkdir(parents=True, exist_ok=True)
    validate(export(args.out), args.samples)
