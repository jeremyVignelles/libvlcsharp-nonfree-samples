// Sample created by Jérémy VIGNELLES
// You can copy, paste, modify, use this file in any of your projects as long as you have supported my work through github sponsors (monthly or one-time payment) : https://github.com/sponsors/jeremyVignelles
// After payment, the file is considered yours and no copyright notice is required (though it would be appreciated).
// The file is provided as-is without any guarantee or support, and you still need to comply to the licenses of the dependencies of this file.

using LibVLCSharp.Shared;
using System.IO.Pipelines;
using ImageSharpMjpegInput;

Core.Initialize();

using var libVLC = new LibVLC("--demux=mjpeg");
var pipe = new Pipe();
using var mediaInput = new PipeMediaInput(pipe.Reader);
using var media = new Media(libVLC, mediaInput);
using var mp = new MediaPlayer(media);

var form = new Form();

form.ClientSize = new Size(800, 600);

form.Load += (s, e) =>
{
    mp.Hwnd = form.Handle;
    mp.Play();
};

form.Show();

var cancellationTokenSource = new CancellationTokenSource();

var producerTask = Task.Run(() => Producer.Run(pipe.Writer, cancellationTokenSource.Token));

form.FormClosing += (s, e) =>
{
    mp.Stop();
    cancellationTokenSource.Cancel();
    Application.Exit();
};

Application.Run();