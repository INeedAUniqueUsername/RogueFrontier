using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoroutineScheduler;
using LibGamer;
using Silk.NET.OpenGLES;

namespace WebGL.Sample;


using DVertex = Vector2;
using DColor = Vector4;
using DTex = Vector2;
using Debug = System.Diagnostics.Debug;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "POD")]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexShaderInput
{
	public DVertex Vertex;
	public DColor Color;
	public DTex Tex;
};
public class Assets {
	private static async Task<HttpContent> Get(HttpClient client, string path) {
		var response = await client.GetAsync(new Uri(path, UriKind.Relative));
		if(!response.IsSuccessStatusCode)
			throw new Exception();
		return response.Content;
	}
	private static async Task<string> GetStr (HttpClient client, string path) => await (await Get(client, path)).ReadAsStringAsync();
	private static async Task<byte[]> GetBytes (HttpClient client, string path) => await (await Get(client, path)).ReadAsByteArrayAsync();
	public string src_vertex;
	public string src_fragment;
	public byte[] tex_missing;
	public async Task Init(Uri baseAddr) {
		var client = new HttpClient() {
			BaseAddress = baseAddr,
		};
		var getStr = async (string s) => await GetStr(client, s);
		var getBytes = async (string s) => await GetBytes(client, s);
		src_vertex = await getStr("shader/Vert.glsl");
		src_fragment = await getStr("shader/Frag.glsl");
		tex_missing = await getBytes("shader/missing.rgba");
	}
}
public class Game {
	private GL gl { get; }
	private Assets assets;
	private Scheduler Scheduler { get; }
	VertexShaderInput[] buf_vertex;
	ushort[] buf_index;
	uint vao, vbo, vbi;
	public unsafe Game(GL gl, Assets assets){
		this.gl = gl;
		this.assets = assets;
	}
	public unsafe void Init () {
			var sh_vertex = gl.CreateShader(ShaderType.VertexShader);
			gl.ShaderSource(sh_vertex, assets.src_vertex);
			gl.CompileShader(sh_vertex);
			gl.GetShader(sh_vertex, ShaderParameterName.CompileStatus, out int res);
			//gl.GetShaderInfoLog(VertexShader, out string log);
			Debug.Assert(res != 0);

			var sh_fragment = gl.CreateShader(ShaderType.FragmentShader);
			gl.ShaderSource(sh_fragment, assets.src_fragment);
			gl.CompileShader(sh_fragment);
			gl.GetShader(sh_fragment, ShaderParameterName.CompileStatus, out res);
			//gl.GetShaderInfoLog(FragmentShader, out log);
			Debug.Assert(res != 0);

				// create the shader
				var ShaderProgram = gl.CreateProgram();
				gl.AttachShader(ShaderProgram, sh_vertex);
				gl.AttachShader(ShaderProgram, sh_fragment);
				gl.LinkProgram(ShaderProgram);
				gl.GetProgram(ShaderProgram, ProgramPropertyARB.LinkStatus, out res);
				gl.UseProgram(ShaderProgram);
				//gl.GetProgramInfoLog(ShaderProgram, out log);
				Debug.Assert(res != 0);

				Debug.Assert(gl.GetError() == GLEnum.NoError, "GetUniformLocation()");

		{
			//Configure shader
			var viewProjectionLoc = gl.GetUniformLocation(ShaderProgram, "viewprojection"u8);
			var vp = Matrix3x2.Identity;
			var matrix = (Span<float>)[
				vp.M11, vp.M21, vp.M31,
						vp.M12, vp.M22, vp.M32
			];
			gl.UniformMatrix2x3(viewProjectionLoc, false, matrix);


		}
		{
			// create the VAO
			vao = gl.GenVertexArray();
			gl.BindVertexArray(vao);
		}
		{
			// setup the vertex buffer to draw
			buf_vertex = new VertexShaderInput[1];
			buf_index = new ushort[1];

			var vbos = (Span<uint>)stackalloc uint[2];
			gl.GenBuffers(vbos);
			vbo = vbos[0];
			vbi = vbos[1];

			int stride = Marshal.SizeOf<VertexShaderInput>();

			gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
			gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(stride * buf_vertex.Length), nint.Zero, BufferUsageARB.StreamDraw);

			gl.EnableVertexAttribArray(0); // vertex
			gl.EnableVertexAttribArray(1); // color
			gl.EnableVertexAttribArray(2); // tex

			var sz_vertex = Marshal.SizeOf<DVertex>();
			var sz_color = Marshal.SizeOf<DColor>();
			var sz_tex = Marshal.SizeOf<DTex>();

			gl.VertexAttribPointer(0, sz_vertex / sizeof(float), VertexAttribPointerType.Float, false, (uint)stride, (void*)(0));
			gl.VertexAttribPointer(1, sz_color / sizeof(float), VertexAttribPointerType.Float, false, (uint)stride, (void*)(sz_vertex));
			gl.VertexAttribPointer(2, sz_tex / sizeof(float), VertexAttribPointerType.Float, false, (uint)stride, (void*)(sz_vertex + sz_color));

			gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, vbi);
			gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(sizeof(ushort) * buf_index.Length), null, BufferUsageARB.StreamDraw);

			gl.BindVertexArray(0);
			Debug.Assert(gl.GetError() is GLEnum.NoError);
		}
		//
		//Make example texture
		var t0 = gl.GenTexture();
		gl.BindTexture(GLEnum.Texture2D, t0);
		fixed(byte* pixels = assets.tex_missing) {
			gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba, 8, 8, 0, GLEnum.Rgba, GLEnum.UnsignedByte, pixels);
		}

		var samplerLoc = gl.GetUniformLocation(ShaderProgram, "uSampler"u8);
		gl.Uniform1(samplerLoc, t0);
		//gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
		//gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
		//gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
		//gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);

		/*
		var pixels = stackalloc int[64];
		var texture = gl.GenTexture();
		gl.BindTexture(GLEnum.Texture2D, texture);
		gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba, 8, 8, 0, GLEnum.Rgba, GLEnum.UnsignedByte, pixels);
		*/
	}
	public unsafe void Render() {
		// iterate our logic thread
		//Scheduler.Resume();
		gl.ClearColor(0f, 0f, 0f, 1.0f);
		gl.Clear(ClearBufferMask.ColorBufferBit);

#if false
		// update the vertex buffer
		var modelMatrix =
			Matrix3x2.CreateScale(Vector2.One) *
			Matrix3x2.CreateRotation(0) *
			Matrix3x2.CreateTranslation(Vector2.Zero);

		for(int i = 0; i < MeshData.TriangleVerts.Length; i++) {
			ref var from = ref MeshData.TriangleVerts[i];
			ref var to = ref buf_vertex[i];
			to = to with {
				Vertex = Vector2.Transform(from.Vertex, modelMatrix),
				Color = from.Color
			};
		}
#endif
		void Render(Sf sf) {

		}

		new Tile(0, 0, 'A');
		var nw = new DVertex(-1, +1);
		var ne = new DVertex(+1, +1);
		var sw = new DVertex(-1, -1);
		var se = new DVertex(+1, -1);
		buf_vertex = [
			new(){Vertex = nw, Color = new(1, 0, 0, 1)},
			new(){Vertex = ne, Color = new(0, 1, 0,1)},
			new(){Vertex = sw, Color = new(0, 0, 1,1)},
			new(){Vertex = se, Color = new(1, 1, 1,1)}
		];
		buf_index = [
			2,1,0,
			//4,3,2
		];
#if false
		for(int i = 0; i < MeshData.TriangleIndices.Length; i++)
			buf_index[i] = MeshData.TriangleIndices[i];
#endif

		void BindVAO () {
			gl.BindVertexArray(vao);
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
			gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, vbi);
			Debug.Assert(gl.GetError() is GLEnum.NoError);
		}
		void UnbindVAO () {
			gl.BindVertexArray(0);
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
			gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
			Debug.Assert(gl.GetError() is GLEnum.NoError);
		}

		BindVAO();
		gl.BufferData<VertexShaderInput>(BufferTargetARB.ArrayBuffer, buf_vertex, BufferUsageARB.StreamDraw);
		gl.BufferData<ushort>(BufferTargetARB.ElementArrayBuffer, buf_index, BufferUsageARB.StreamDraw);
		gl.DrawElements(PrimitiveType.Triangles, (uint)buf_index.Length, DrawElementsType.UnsignedShort, (void*)0);
		UnbindVAO();

	}
	public void CanvasResized(int width, int height) {
		gl.Viewport(0, 0, (uint)width, (uint)height);
		// note: in a real game, aspect ratio corrections should be applies
		// to your projection transform, not your model transform
		//LogoScale = new Vector2(height / (float)width, 1.0f);
	}
}
