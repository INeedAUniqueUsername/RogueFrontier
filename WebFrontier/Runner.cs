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
using QuikGraph.Algorithms.MaximumFlow;
using System.Runtime.CompilerServices;
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

	public byte[] rf_8x8, ibmcga_6x8;
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
		rf_8x8 = await getBytes("Assets/font/RF_8x8.rgba");
		ibmcga_6x8 = await getBytes("Assets/font/IBMCGA+_6x8.rgba");
		return new GetDataAsync(getStr, getBytes);
	}
}
record TexMaker(GL gl) {
	GLEnum unit = GLEnum.Texture0;
	int slot = 0;
	public unsafe TexInfo MakeTexture (byte[] data, uint w, uint h) {
		gl.ActiveTexture(unit);
		var t = gl.GenTexture();

		gl.BindTexture(GLEnum.Texture2D, t);
		gl.TexStorage2D(GLEnum.Texture2D, 1, GLEnum.Rgba8, w, h);
		fixed(byte* pixels = data)
			gl.TexSubImage2D(GLEnum.Texture2D, 0, 0, 0, w, h, GLEnum.Rgba, GLEnum.UnsignedByte, pixels);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapR, (int)GLEnum.ClampToEdge);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);

		Runner.CheckError(gl, "texture");

		var r = new TexInfo(slot);

		unit += 1;
		slot += 1;
		return r;
	}
}
record TexInfo(int slot) {
	public void BindSampler(GL gl, uint program, string uniform) {
		gl.Uniform1(gl.GetUniformLocation(program, uniform), slot);
	}
}

public class Runner {



	private GL gl { get; }
	private Scheduler Scheduler { get; }

	uint vao, vbo, vbi;
	public Runner(GL gl){
		this.gl = gl;
	}




	public unsafe void Init (Downloader assets) {
		Console.WriteLine($"A");

		gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
		gl.Enable(GLEnum.Blend);

		var iProgram = gl.CreateProgram();
		
		MakeShader(ShaderType.VertexShader, assets.src_vertex);
		MakeShader(ShaderType.FragmentShader, assets.src_fragment);

		gl.LinkProgram(iProgram);
		gl.GetProgram(iProgram, ProgramPropertyARB.LinkStatus, out var res);
		if(res == 0) {
			gl.GetProgramInfoLog(iProgram, out string info);
			Console.WriteLine(info);
			Debug.Assert(res != 0, $"GetProgram error: {res}");
		}
		gl.UseProgram(iProgram);

		CheckError(gl, "GetUniformLocation()");

		Console.WriteLine($"C"); {
			//Configure shader
			var vp = Matrix3x2.Identity;

			var matrix = (Span<float>)[
				vp.M11, vp.M21, vp.M31,
				vp.M12, vp.M22, vp.M32
			];
			var viewProjectionLoc = gl.GetUniformLocation(iProgram, "viewprojection"u8);
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
		
		BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
		BindBuffer(BufferTargetARB.ElementArrayBuffer, vbi);
		Console.WriteLine("F");

		var vap = (
			stride: (uint)Marshal.SizeOf<VertexShaderInput>(),
			index: 0u,
			pointer: 0,
			add: default(Action<int>)
		);
		vap = vap with {
			add = (int sz) => {
				gl.EnableVertexAttribArray(vap.index);
				gl.VertexAttribPointer(vap.index, sz / sizeof(float), VertexAttribPointerType.Float, false, vap.stride, (void*)vap.pointer);
				vap.index += 1;
				vap.pointer += sz;
			}
		};
		Enumerable.ToList([
			Marshal.SizeOf<DVertex>(),
			Marshal.SizeOf<DColor>(),
			Marshal.SizeOf<DTex>()
		]).ForEach(vap.add);

		gl.BindVertexArray(0);
		CheckError(gl, "VertexAttribPointer");

		Console.WriteLine("G");

		TexMaker tm = new(gl);
		
		var t0 = tm.MakeTexture(assets.rf_8x8, 256, 256);
		var t1 = tm.MakeTexture(assets.ibmcga_6x8, 192, 256);

		Console.WriteLine("H");
		t0.BindSampler(gl, iProgram, "uSampler");
		CheckError(gl, "uSampler");

		void BindBuffer (BufferTargetARB target, uint i) {
			gl.BindBuffer(target, i);
			gl.BufferData(target, 0, 0, BufferUsageARB.StreamDraw);
		};
		void MakeShader (ShaderType type, string src) {
			var shader = gl.CreateShader(type);
			gl.ShaderSource(shader, src);
			gl.CompileShader(shader);
			gl.GetShader(shader, ShaderParameterName.CompileStatus, out var res);
			if(res == 0) {
				gl.GetShaderInfoLog(shader, out string info);
				Console.WriteLine(info);
				Debug.Assert(res != 0, $"shader: {src}");
			}
			gl.AttachShader(iProgram, shader);
		};
	}

	private IScene _current;
	public IScene current {
		get => _current;
		set {
			if(_current is { } prev) {
				prev.Draw -= Draw;
				prev.Go -= Go;
			}
			_current = value;
			if(value is { } next) {
				next.Draw += Draw;
				next.Go += Go;
			}
			void Draw(Sf sf) {
				RenderSf(sf);
			}
			void Go(IScene to) {
				current = to;
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
		DColor VecABGR (uint c) =>
			new(ABGR.R(c) / 255f, ABGR.G(c) / 255f, ABGR.B(c) / 255f, ABGR.A(c) / 255f);
		var scale = (float)sf.scale  * 1f;
		var szPixelVert = new DVertex(scale, scale) / new DVertex(width, height);
		var szTileVert = new DVertex(sf.GlyphWidth, sf.GlyphHeight) * szPixelVert;
		var szTileTex = new DTex(sf.font.GlyphWidth, sf.font.GlyphHeight) / new DVertex(sf.font.ImageWidth, sf.font.ImageHeight);
		foreach(var posTile in sf.Positions) {
			var posTileVert = new DVertex(
				2f * posTile.x * szTileVert.X - 1f,
				-(2f * posTile.y * szTileVert.Y - 1f)
				) - szTileVert * new DVertex(0, 2);
			var t = sf.Tile[(posTile.x, posTile.y)];

			var back = VecABGR(t.Background);
			var backTex = VecXYI(sf.font.GetGlyphPos(sf.font.solidGlyphIndex)) / VecXYI(sf.font.GridSize);
			canvas.AddSquare((posTileVert, szTileVert), back, (backTex, szTileTex));
			var front = VecABGR(t.Foreground);
			var frontTex = VecXYI(sf.font.GetGlyphPos((int)t.Glyph)) / VecXYI(sf.font.GridSize);			
			canvas.AddSquare((posTileVert, szTileVert), front, (frontTex, szTileTex));
			//var fontPos = Vec(sf.font.GetGlyphPos((int)t.Glyph)) * tex_size;
		}
	}
	KB kb = new();
	Canvas canvas = new();
	DateTime prevRender = DateTime.Now;
	public unsafe void Update() {
		// iterate our logic thread
		//Scheduler.Resume();
		gl.ClearColor(0f, 0f, 0f, 1.0f);
		gl.Clear(ClearBufferMask.ColorBufferBit);


		var delta = (DateTime.Now - prevRender);
		kb.Update(Interop.down);
		Console.WriteLine(string.Join(",", kb.Down));

		canvas.Clear();
		current?.Update(delta);
		current?.HandleKey(kb);
		current?.HandleMouse(Interop.hand);
		current?.Render(delta);
		prevRender = DateTime.Now;
		
		var buf_vertex = CollectionsMarshal.AsSpan(canvas.li_vertex);
		var buf_index = CollectionsMarshal.AsSpan(canvas.li_index);

		BindVAO();
		gl.BufferData<VertexShaderInput>(BufferTargetARB.ArrayBuffer, buf_vertex, BufferUsageARB.StreamDraw);
		gl.BufferData<ushort>(BufferTargetARB.ElementArrayBuffer, buf_index, BufferUsageARB.StreamDraw);
		gl.DrawElements(PrimitiveType.Triangles, (uint)buf_index.Length, DrawElementsType.UnsignedShort, (void*)0);
		CheckError(gl, "DrawElements");
		UnbindVAO();
		void Bind (uint vao, uint vbo, uint vbi) {
			gl.BindVertexArray(vao);
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
			gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, vbi);
			CheckError(gl, $"{vao}, {vbo}, {vbi}");
		};
		void UnbindVAO () => Bind(0, 0, 0);
		void BindVAO () => Bind(vao, vbo, vbi);

	}
	public static void CheckError (GL gl, string msg = null) {
		var err = gl.GetError();
		if(err != GLEnum.NoError) {
			var s = $"GL error: {err}";
			var pre = msg is { } ? $"[{msg}] " : "";
			Console.WriteLine($"{pre}{s}");
		}
		Debug.Assert(err is GLEnum.NoError, msg);
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
