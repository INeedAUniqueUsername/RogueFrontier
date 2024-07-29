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
using RogueFrontier;
namespace WebAtomics;

using DVertex = Vector2;
using DColor = Vector4;
using DTex = Vector2;
using Vec2 = Vector2;
using Vec4 = Vector4;
using Debug = System.Diagnostics.Debug;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "POD")]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexShaderInput
{
	public Vec2 xy_pos;
	public Vec2 tex_pos;
	public Vec4 rgba;
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
		Runner.CheckError(gl, "uSampler");
	}
}

public class Runner {



	private GL gl { get; }
	private Scheduler Scheduler { get; }

	TexMaker tm;
	public Runner(GL gl){
		this.gl = gl;
		tm = new TexMaker(gl);
	}


	uint iProgram;

	public unsafe void Init (Downloader assets) {
		Console.WriteLine($"A");

		gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
		gl.Enable(GLEnum.Blend);

		iProgram = gl.CreateProgram();
		
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

		CheckError(gl, "UseProgram()");

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
	Dictionary<Tf, (uint vao, uint vbo, TexInfo tex)> fontData = [];
	record VapMaker (GL gl) {
		uint stride = (uint)Marshal.SizeOf<VertexShaderInput>();
		uint index = 0;
		int pointer = 0;
		public unsafe void add (int sz, uint divisor = 0) {
			gl.EnableVertexAttribArray(index);
			gl.VertexAttribPointer(index, sz / sizeof(float), VertexAttribPointerType.Float, false, stride, (void*)pointer);
			if(divisor > 0)
				gl.VertexAttribDivisor(index, divisor);

			CheckError(gl, $"Attribute {index}");
			index += 1;
			pointer += sz;
		}
	}
	unsafe void RenderSf (Sf sf) {
		void BindBuffer (BufferTargetARB target, uint i) {
			gl.BindBuffer(target, i);
			gl.BufferData(target, 0, 0, BufferUsageARB.StreamDraw);
		};
		Vec2 VecXYI ((int x, int y) p) =>
			new(p.x, p.y);
		Vec4 VecABGR (uint c) =>
			new(ABGR.R(c) / 255f, ABGR.G(c) / 255f, ABGR.B(c) / 255f, ABGR.A(c) / 255f);
		if(!fontData.TryGetValue(sf.font, out var data)) {
			Console.WriteLine($"Initialize {sf.font.name}");
			data.vao = gl.GenVertexArray();
			gl.BindVertexArray(data.vao);
			CheckError(gl, "BindVAO");
			data.vbo = gl.GenBuffer();
			BindBuffer(BufferTargetARB.ArrayBuffer, data.vbo);
			CheckError(gl, "BindVBO");
			VapMaker vap = new VapMaker(gl);

			CheckError(gl, "Attribute");
			Enumerable.ToList([
				Marshal.SizeOf<Vec2>(),
				Marshal.SizeOf<Vec2>(),
				Marshal.SizeOf<Vec4>()
			]).ForEach(i => vap.add(i, 1));

			gl.BindVertexArray(0);
			CheckError(gl, "VertexAttribPointer");

			data.tex = tm.MakeTexture(sf.font.rgba, (uint)sf.font.ImageWidth, (uint)sf.font.ImageHeight);
			fontData[sf.font] = data;
		}
		gl.BindVertexArray(data.vao);
		data.tex.BindSampler(gl, iProgram, "uSampler");
		var scale = (float)sf.scale * 1f;
		var szPixelVert = new Vec2(scale, scale) / new DVertex(width, height);
		var szTileVert = VecXYI(sf.GlyphSize) * szPixelVert;
		var szTileTex = VecXYI(sf.font.GlyphSize) / VecXYI(sf.font.ImageSize);
		gl.Uniform2(gl.GetUniformLocation(iProgram, "in_xy_size"u8), szTileVert);
		gl.Uniform2(gl.GetUniformLocation(iProgram, "in_tex_size"u8), szTileTex);
		var buf = new List<VertexShaderInput>(sf.TileCount * 2);
		//canvas.AddSquare((posTileVert, szTileVert), (backTex, szTileTex));
		foreach(var posTile in sf.Positions) {
			var posTileVert = new Vec2(
				2f * posTile.x * szTileVert.X - 1f,
				-(2f * posTile.y * szTileVert.Y - 1f)
				) - szTileVert * new Vec2(0, 2);
			var t = sf.Tile[(posTile.x, posTile.y)];

			var back = VecABGR(t.Background);
			var backTex = VecXYI(sf.font.GetGlyphPos(sf.font.solidGlyphIndex)) / VecXYI(sf.font.GridSize);
			buf.Add(new() {
				xy_pos = posTileVert,
				tex_pos = backTex,
				rgba = back
			});
			var front = VecABGR(t.Foreground);
			var frontTex = VecXYI(sf.font.GetGlyphPos((int)t.Glyph)) / VecXYI(sf.font.GridSize);
			buf.Add(new() {
				xy_pos = posTileVert,
				tex_pos = frontTex,
				rgba = front
			});
		}
		var buf_input = CollectionsMarshal.AsSpan(buf);
		gl.BindBuffer(GLEnum.ArrayBuffer, data.vbo);
		gl.BufferData<VertexShaderInput>(GLEnum.ArrayBuffer, buf_input, GLEnum.StreamDraw);
		//Console.WriteLine("DrawSf");
		//Console.WriteLine("BindSampler");
		gl.DrawArraysInstanced(GLEnum.TriangleStrip, 0, 4, (uint)sf.TileCount * 2);
		//Console.WriteLine("DrawArraysInstanced");
		gl.BindVertexArray(0);
		//Console.WriteLine("BindVertexArray(0)");
	}
	KB kb = new();
	DateTime prevRender = DateTime.Now;
	public unsafe void Update() {
		// iterate our logic thread
		//Scheduler.Resume();
		gl.ClearColor(0f, 0f, 0f, 1.0f);
		gl.Clear(ClearBufferMask.ColorBufferBit);
		kb.Update(Interop.down);
		Console.WriteLine(string.Join(",", kb.Down));
		var delta = (DateTime.Now - prevRender);
		current?.Update(delta);
		current?.HandleKey(kb);
		current?.HandleMouse(Interop.hand);
		current?.Render(delta);
		prevRender = DateTime.Now;
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
