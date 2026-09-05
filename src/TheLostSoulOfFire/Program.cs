using System;
using TheLostSoulOfFire.Debugging;

if (!VisualRunOptions.TryParse(args, out VisualRunOptions visualOptions, out string visualOptionError))
{
    Console.Error.WriteLine($"VISUAL_RUN_ARGUMENT_ERROR {visualOptionError}");
    Environment.ExitCode = 2;
    return;
}

bool audioRuntimeTest = Array.Exists(args, argument => argument == "--audio-runtime-test");
bool audioLoopRuntimeTest = Array.Exists(args, argument => argument == "--audio-loop-runtime-test");
bool audioGameplayTest = Array.Exists(args, argument => argument == "--audio-gameplay-test");
bool audioDeathRestartTest = Array.Exists(args, argument => argument == "--audio-death-restart-test");
bool expectAudioFallback = Array.Exists(args, argument => argument == "--expect-audio-fallback");

using Microsoft.Xna.Framework.Game game = audioRuntimeTest || audioLoopRuntimeTest
    ? new AudioRuntimeTestGame(expectAudioFallback, audioLoopRuntimeTest)
    : new TheLostSoulOfFire.Game1(audioGameplayTest, audioDeathRestartTest, visualOptions);
game.Run();
