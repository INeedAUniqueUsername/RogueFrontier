using System;
using System.Diagnostics;
using System.Net.Http;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using CoroutineScheduler;

using Silk.NET.OpenGLES;

namespace WebGL.Sample;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "POD")]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexShaderInput
{
	public Vector2 Vertex;
	public Vector3 Color;
};

public class MeshDemo
{
	private GL Gl { get; }
	private Scheduler Scheduler { get; }
	
	private static async Task<string> DownloadFile(
		HttpClient client,
		string path) {
		var response = await client.GetAsync(new Uri(path, UriKind.Relative));
		if (!response.IsSuccessStatusCode)
			throw new Exception();
		return await response.Content.ReadAsStringAsync();
	}
	public static MeshDemo LoadAsync (GL gl, Uri baseAddress) {
		var client = new HttpClient() {
			BaseAddress = baseAddress,
		};


		return new MeshDemo(gl);
	}
	public unsafe  MeshDemo(
		GL gl){
		Gl = gl;

		var pixels = stackalloc int[64];
		var texture = gl.GenTexture();
		gl.BindTexture(GLEnum.Texture2D, texture);
		gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba, 8, 8, 0, GLEnum.Rgba, GLEnum.UnsignedByte, pixels);
	}

	float t = 0.5f;
	public unsafe void Render() {
		// iterate our logic thread
		//Scheduler.Resume();

		t = (float)Math.IEEERemainder(t + 0.01, 1);
		// dispatch GL commands
		//var t = 0.5f;
		Gl.ClearColor(t, t, t, 1.0f);
		Gl.Clear(ClearBufferMask.ColorBufferBit);



		
	}

	internal void CanvasResized(int width, int height)
	{
		Gl.Viewport(0, 0, (uint)width, (uint)height);

		// note: in a rea lgame, aspect ratio corrections should be applies
		// to your projection transform, not your model transform
		//LogoScale = new Vector2(height / (float)width, 1.0f);
	}
}
