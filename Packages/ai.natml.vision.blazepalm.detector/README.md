# BlazePalm Detector
Palm detection from Google MediaPipe in Unity Engine.

## Installing BlazePalm Detector
Add the following items to your Unity project's `Packages/manifest.json`:
```json
{
  "scopedRegistries": [
    {
      "name": "NatML",
      "url": "https://registry.npmjs.com",
      "scopes": ["ai.natml"]
    }
  ],
  "dependencies": {
    "ai.natml.vision.blazepalm.detector": "1.0.1"
  }
}
```

## Detecting Hands in an Image
First, create the detector:
```csharp
// Create the BlazePalm detector
var detector = await BlazePalmDetector.Create();
```

Finally, detect hands in an image:
```csharp
Texture2D image = ...;
// Detect hands in the image
BlazePalmDetector.Detection[] hands = detector.Predict(image);
```

___

## Requirements
- Unity 2021.2+

## Quick Tips
- Join the [NatML community on Discord](https://natml.ai/community).
- Discover more ML models on [NatML Hub](https://hub.natml.ai).
- See the [NatML documentation](https://docs.natml.ai/unity).
- Contact us at [hi@natml.ai](mailto:hi@natml.ai).

Thank you very much!