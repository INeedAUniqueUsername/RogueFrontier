using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoroutineScheduler;
using LibGamer;
using Silk.NET.OpenGLES;
using LibAtomics;
namespace WebAtomics;

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
public class Downloader {
	private static async Task<HttpContent> Get(HttpClient client, string path) {
		var response = await client.GetAsync(new Uri(path, UriKind.Relative));
		if(!response.IsSuccessStatusCode)
			throw new Exception();
		return response.Content;
	}
	public string src_vertex;
	public string src_fragment;
	public byte[] tex_missing;
	public bool[] tex_missing_b;

	public byte[] ibmcga_8x8;
	public bool[] ibmcga_8x8_b;
	public async Task<GetDataAsync> Init(Uri baseAddr) {
		var client = new HttpClient() {
			BaseAddress = baseAddr,
		};
		var getStr = async (string s) => {
			Console.WriteLine($"Downloading {s} from server");
			return await (await Get(client, s)).ReadAsStringAsync();
		};
		var getBytes = async (string s) => {
			Console.WriteLine($"Downloading {s} from server");
			return await (await Get(client, s)).ReadAsByteArrayAsync();
		};
		src_vertex = await getStr("shader/vertex.glsl");
		src_fragment = await getStr("shader/fragment.glsl");
		tex_missing = await getBytes("shader/missing.rgba");
		tex_missing_b = [..from i in tex_missing.Length/4 select tex_missing[i*4] != 0];
		ibmcga_8x8 = await getBytes("Assets/font/RF_8x8.rgba");
		ibmcga_8x8_b = [..from i in ibmcga_8x8.Length / 4 select ibmcga_8x8[i * 4] != 0];
		return new GetDataAsync(getStr, getBytes);
	}
}
public class Runner {
	private GL gl { get; }
	private Scheduler Scheduler { get; }
	VertexShaderInput[] buf_vertex;
	ushort[] buf_index;
	uint vao, vbo, vbi;
	public Runner(GL gl){
		this.gl = gl;
	}
	public unsafe void Init (Downloader assets) {
		Console.WriteLine($"A");
		gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
		gl.Enable(GLEnum.Blend);
		var iProgram = gl.CreateProgram();
		var sh_vertex = gl.CreateShader(ShaderType.VertexShader);
		gl.ShaderSource(sh_vertex, assets.src_vertex);
		gl.CompileShader(sh_vertex);
		gl.GetShader(sh_vertex, ShaderParameterName.CompileStatus, out int res);
		//gl.GetShaderInfoLog(VertexShader, out string log);
		if(res == 0) {
			gl.GetShaderInfoLog(sh_vertex, out string info);
			Console.WriteLine(info);
			Debug.Assert(res != 0, "sh_vertex");
		}
		gl.AttachShader(iProgram, sh_vertex);

		var sh_fragment = gl.CreateShader(ShaderType.FragmentShader);
		gl.ShaderSource(sh_fragment, assets.src_fragment);
		gl.CompileShader(sh_fragment);
		gl.GetShader(sh_fragment, ShaderParameterName.CompileStatus, out res);
		//gl.GetShaderInfoLog(FragmentShader, out log);
		if(res == 0) {
			gl.GetShaderInfoLog(sh_fragment, out string info);
			Console.WriteLine(info);
			Debug.Assert(res != 0, "sh_fragment");
		}
		gl.AttachShader(iProgram, sh_fragment);
		gl.LinkProgram(iProgram);
		gl.GetProgram(iProgram, ProgramPropertyARB.LinkStatus, out res);
		if(res == 0) {
			gl.GetProgramInfoLog(iProgram, out string info);
			Console.WriteLine(info);
			Debug.Assert(res != 0, $"GetProgram error: {res}");
		}
		gl.UseProgram(iProgram);
		//gl.GetProgramInfoLog(ShaderProgram, out log);
		Debug.Assert(gl.GetError() == GLEnum.NoError, "GetUniformLocation()");

		Console.WriteLine($"C");
		{
			//Configure shader
			var viewProjectionLoc = gl.GetUniformLocation(iProgram, "viewprojection"u8);
			var vp = Matrix3x2.Identity;
			var matrix = (Span<float>)[
				vp.M11, vp.M21, vp.M31,
						vp.M12, vp.M22, vp.M32
			];
			gl.UniformMatrix2x3(viewProjectionLoc, false, matrix);
		}

		// setup the vertex buffer to draw
		Console.WriteLine("D");
		vao = gl.GenVertexArray();
		gl.BindVertexArray(vao);

		var vbos = (Span<uint>)stackalloc uint[2];
		gl.GenBuffers(vbos);
		vbo = vbos[0];
		vbi = vbos[1];

		buf_vertex = new VertexShaderInput[1];
		buf_index = new ushort[1];

		int stride = Marshal.SizeOf<VertexShaderInput>();
		gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
		gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(stride * buf_vertex.Length), nint.Zero, BufferUsageARB.StreamDraw);
		gl.EnableVertexAttribArray(0); // vertex
		gl.EnableVertexAttribArray(1); // color
		gl.EnableVertexAttribArray(2); // tex

		Console.WriteLine("F");
		var sz_vertex = Marshal.SizeOf<DVertex>();
		var sz_color = Marshal.SizeOf<DColor>();
		var sz_tex = Marshal.SizeOf<DTex>();
		gl.VertexAttribPointer(0, sz_vertex / sizeof(float), VertexAttribPointerType.Float, false, (uint)stride, (void*)(0));
		gl.VertexAttribPointer(1, sz_color / sizeof(float), VertexAttribPointerType.Float, false, (uint)stride, (void*)(sz_vertex));
		gl.VertexAttribPointer(2, sz_tex / sizeof(float), VertexAttribPointerType.Float, false, (uint)stride, (void*)(sz_vertex + sz_color));

		gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, vbi);
		gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(sizeof(ushort) * buf_index.Length), null, BufferUsageARB.StreamDraw);

		gl.BindVertexArray(0);
		Debug.Assert(gl.GetError() is GLEnum.NoError, "VertexAttribPointer");

		Console.WriteLine("G");
		var t = gl.GenTexture();
		gl.ActiveTexture(GLEnum.Texture0);
		gl.BindTexture(GLEnum.Texture2D, t);
		gl.TexStorage2D(GLEnum.Texture2D, 1, GLEnum.Rgba8, 256, 256);
		fixed(byte* pixels = assets.ibmcga_8x8)
			gl.TexSubImage2D(GLEnum.Texture2D, 0, 0, 0, 256, 256, GLEnum.Rgba, GLEnum.UnsignedByte, pixels);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapR, (int)GLEnum.ClampToEdge);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
		//gl.GenerateMipmap(GLEnum.Texture2D);
		CheckError(gl, "texture");
		Console.WriteLine("H");
		gl.Uniform1(gl.GetUniformLocation(iProgram, "uSampler"u8), 0);
		CheckError(gl, "uSampler");

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

	private IScene _current;
	public IScene current {
		get => _current;
		set {
			if(_current is { } prev) {
				prev.Draw -= Draw;
			}
			_current = value;
			if(value is { } next) {
				next.Draw += Draw;
			}
			void Draw(Sf sf) {
				RenderSf(sf);
			}
		}
	}

	record Canvas {
		public List<VertexShaderInput> li_vertex = [];
		public List<ushort> li_index = [];

		public void Clear () {
			li_vertex.Clear();
			li_index.Clear();
		}
		public void AddPolygon (VertexShaderInput[] inp, ushort[] ind) {
			var sz = li_vertex.Count;
			li_index.AddRange(from i in ind select (ushort)(i + sz));
			li_vertex.AddRange(inp);
		}
		public void AddSquare ((DVertex pos, DVertex size) vertex, DColor color, (DTex pos, DTex size) tex) {
			AddPolygon([
				new(){ Vertex = vertex.pos + 2*vertex.size*new DVertex(0,0), Color = color, Tex = tex.pos + tex.size*new DTex(0, 1) }, //nw
				new(){ Vertex = vertex.pos + 2*vertex.size*new DVertex(1,0), Color = color, Tex = tex.pos + tex.size*new DTex(1, 1) }, //ne
				new(){ Vertex = vertex.pos + 2*vertex.size*new DVertex(0,1), Color = color, Tex = tex.pos + tex.size*new DTex(0, 0) }, //sw
				new(){ Vertex = vertex.pos + 2*vertex.size*new DVertex(1,1), Color = color, Tex = tex.pos + tex.size*new DTex(1, 0) }, //se
			], [
				0,1,2,
				1,2,3
			]);
		}
	}

	

	void RenderSf (Sf sf) {
		DVertex VecXYI ((int x, int y) p) =>
			new(p.x, p.y);
		DColor VecABGR (ABGR from) =>
			new DColor(from.r / 255f, from.g / 255f, from.b / 255f, from.a / 255f);

		var scale = (float)sf.scale;
		var szPixelVert = new DVertex(scale / width, scale / height);
		var szTileVert = new DVertex(sf.GlyphWidth, sf.GlyphHeight) * szPixelVert;
		var szTileTex = new DTex(1f * sf.font.GlyphWidth / sf.font.ImageWidth, 1f * sf.font.GlyphHeight / sf.font.ImageHeight);
		foreach(var posTile in sf.Positions) {
			var posTileVert = new DVertex(
				2f * posTile.x * szTileVert.X - 1f,
				2f * posTile.y * szTileVert.Y - 1f
				);
			var t = sf.Tile[(posTile.x, sf.GridHeight - posTile.y-1)];
			var back = VecABGR(new ABGR(t.Background));

			var backTex = VecXYI(sf.font.GetGlyphPos(sf.font.solidGlyphIndex)) / VecXYI(sf.font.GridSize);
			canvas.AddSquare((posTileVert, szTileVert), back, (backTex, szTileTex));

			var front = VecABGR(new ABGR(t.Foreground));
			var frontTex = VecXYI(sf.font.GetGlyphPos((int)t.Glyph)) / VecXYI(sf.font.GridSize);
			//var fontPos = Vec(sf.font.GetGlyphPos((int)t.Glyph)) * tex_size;

			canvas.AddSquare((posTileVert, szTileVert), front, (frontTex, szTileTex));
		}
	}

	KB kb = new();
	Canvas canvas = new();

	DateTime prevRender = DateTime.Now;
	public unsafe void Render() {
		// iterate our logic thread
		//Scheduler.Resume();
		gl.ClearColor(0f, 0f, 0f, 1.0f);
		gl.Clear(ClearBufferMask.ColorBufferBit);

		canvas.Clear();

		var now = DateTime.Now;
		var delta = (now - prevRender);
		kb.Update(Interop.down);

		Console.WriteLine(string.Join(",", Interop.down));
		current?.Update(delta);
		current?.HandleKey(kb);
		current?.HandleMouse(Interop.hand);
		current?.Render(delta);

		prevRender = now;

		buf_vertex = [.. canvas.li_vertex];
		buf_index = [.. canvas.li_index];
		/*
		var nw = new DVertex(-1, +1);
		var ne = new DVertex(+1, +1);
		var sw = new DVertex(-1, -1);
		var se = new DVertex(+1, -1);
		buf_vertex = [
			new(){Vertex = nw, Color = new(1, 0, 0, 1), Tex = new(0, 0)},
			new(){Vertex = ne, Color = new(0, 1, 0,1), Tex = new(1, 0)},
			new(){Vertex = sw, Color = new(0, 0, 1,1), Tex = new(0, 1)},
			new(){Vertex = se, Color = new(1, 1, 1,1), Tex = new(1, 1)}
		];
		buf_index = [
			2,1,0,
			//4,3,2
		];
		*/
#if false
		for(int i = 0; i < MeshData.TriangleIndices.Length; i++)
			buf_index[i] = MeshData.TriangleIndices[i];
#endif
		void Bind(uint vao, uint vbo, uint vbi) {
			gl.BindVertexArray(vao);
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
			gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, vbi);

			CheckError(gl, $"{vao}, {vbo}, {vbi}");
		}
		void UnbindVAO () => Bind(0, 0, 0);
		void BindVAO () => Bind(vao, vbo, vbi);
		BindVAO();
		gl.BufferData<VertexShaderInput>(BufferTargetARB.ArrayBuffer, buf_vertex, BufferUsageARB.StreamDraw);
		gl.BufferData<ushort>(BufferTargetARB.ElementArrayBuffer, buf_index, BufferUsageARB.StreamDraw);
		gl.DrawElements(PrimitiveType.Triangles, (uint)buf_index.Length, DrawElementsType.UnsignedShort, (void*)0);
		CheckError(gl, "DrawElements");
		UnbindVAO();
	}

	static void CheckError (GL gl, string msg = null) {
		var err = gl.GetError();
		if(err != GLEnum.NoError) {
			var s = $"GL error: {err}";
			var pre = msg is { } ? $"[{msg}] " : "";
			Console.WriteLine($"{pre}{s}");
		}
		Debug.Assert(err is GLEnum.NoError);
	}

	int width, height;
	public void CanvasResized(int width, int height) {
		(this.width, this.height) = (width, height);

		//Console.WriteLine($"Canvas size: {width},{height}");
		gl.Viewport(0, 0, (uint)width, (uint)height);
		// note: in a real game, aspect ratio corrections should be applies
		// to your projection transform, not your model transform
		//LogoScale = new Vector2(height / (float)width, 1.0f);
	}
}
