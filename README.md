# Unity Vosk Speech-to-Text for Apple Silicon

This project implements real-time speech-to-text functionality in Unity using the Vosk speech recognition toolkit, optimized for Apple Silicon Macs.

## Features

- Real-time speech recognition
- Support for multiple microphones
- Customizable speech detection parameters
- Threaded audio processing for improved performance

## Prerequisites

- Unity 2022.3 or later
- macOS with Apple Silicon (M1 chip or later)
- Xcode (for building on macOS)

## Setup

1. Clone this repository or download the project files.

2. Open the project in Unity.

3. Download the Vosk model:
   - Go to the [Vosk Models page](https://alphacephei.com/vosk/models)
   - Download the `vosk-model-small-en-us-0.15` model (or another model of your choice)
   - Extract the downloaded model to `Assets/StreamingAssets/models/`

4. Ensure the Vosk library is properly set up:
   - Check that `libvosk.dylib` is present in `Assets/Plugins/macOS/`
   - Verify that the `VoskLoader.cs` script is in your project

## Usage

1. Add the `ImprovedSpeechToText` script to a GameObject in your scene.

2. Configure the script in the Inspector:
   - Select a microphone from the available list
   - Adjust the silence threshold, minimum speech duration, and maximum silence duration as needed

3. Run the scene. The script will automatically start listening and processing speech.

4. Speech recognition results will be logged to the Console. You can modify the `ProcessRecognitionResult` method to handle the results as needed for your application.

## Customization

- To use a different Vosk model, change the `modelName` variable in the `ImprovedSpeechToText` script.
- Adjust the `silenceThreshold`, `minSpeechDuration`, and `maxSilenceDuration` parameters to fine-tune speech detection.

## Troubleshooting

- If you encounter issues with library loading, check the Console for error messages from the `VoskLoader` script.
- Ensure that the Vosk model is correctly placed in the StreamingAssets folder.
- Verify that your microphone is properly connected and recognized by your system.

## License

[MIT License]

## Acknowledgements

This project uses the [Vosk Speech Recognition Toolkit](https://github.com/alphacep/vosk-api), which is distributed under the Apache 2.0 license.

