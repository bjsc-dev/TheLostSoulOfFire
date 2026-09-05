using System;
using TheLostSoulOfFire.Debugging;

bool audioRuntimeTest = Array.Exists(args, argument => argument == "--audio-runtime-test");
bool audioLoopRuntimeTest = Array.Exists(args, argument => argument == "--audio-loop-runtime-test");
bool audioGameplayTest = Array.Exists(args, argument => argument == "--audio-gameplay-test");
bool audioDeathRestartTest = Array.Exists(args, argument => argument == "--audio-death-restart-test");
bool expectAudioFallback = Array.Exists(args, argument => argument == "--expect-audio-fallback");

if (!VisualCaptureOptions.TryParse(args, out VisualCaptureOptions visualCapture, out string captureError))
{
    Console.Error.WriteLine($"Visual capture argument error: {captureError}");
    Console.Error.WriteLine(VisualCaptureOptions.Usage);
    Environment.ExitCode = 2;
    return;
}

if (visualCapture is not null && (audioRuntimeTest || audioLoopRuntimeTest || audioGameplayTest || audioDeathRestartTest))
{
    Console.Error.WriteLine("Visual capture options cannot be combined with an automated audio test mode.");
    Environment.ExitCode = 2;
    return;
}

using Microsoft.Xna.Framework.Game game = audioRuntimeTest || audioLoopRuntimeTest
    ? new AudioRuntimeTestGame(expectAudioFallback, audioLoopRuntimeTest)
    : new TheLostSoulOfFire.Game1(audioGameplayTest, audioDeathRestartTest, visualCapture);
game.Run();
