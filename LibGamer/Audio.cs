using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace LibGamer;
public class Audio {
	public static byte[] ParseWav (byte[] d) {
		var i = 0;
		int ToInt (byte[] bytes) => bytes.Select((b,i) => b << (i * 8)).Sum();
		int Read (int index, int len = 4) => ToInt(d[index..(index + len)]);
		int format;
		int sample_rate;
		int bits_per_sample = 0;
		bool stereo = false;
		byte[] samples = [];
		var loopEnd = 0;
		var loopMode = 1;
		Step:
		var header = new string([.. d[..4].Select(b => (char)b)]);
		switch(header) {
			case "RIFF":
				break;
			case "WAVE":
				break;
			case "fmt": {

					//var subchunk_sz = d[i + 4] + (d[i + 5] << 8) + (d[i + 6] << 16) + (d[i + 7] << 24);
					var subchunk_sz = Read(i + 4);

					var fsc0 = i + 8;

					//var format_code = d[fsc0] + (d[fsc0 + 1] << 8);
					var format_code = Read(fsc0, 2);

					//var channel_count = d[fsc0 + 2] + (d[fsc0 + 3] << 8);
					var channel_count = Read(fsc0 + 2, 2);

					sample_rate = Read(fsc0 + 4, 4);
					var byte_rate = Read(fsc0 + 8);
					var bits_sample_channel = Read(fsc0 + 12, 2);

					bits_per_sample = Read(fsc0 + 14, 2);

					break;
				}
			case "data": {
					var audio_data_size = Read(i + 4, 4);
					var data_start = i + 8;
					var data = d[data_start..(data_start + audio_data_size)];
					if(Enumerable.Contains([24,32], bits_per_sample)) {
						samples = Convert(data, bits_per_sample);
					} else {
						samples = data;
					}
					break;
				}
		}
		i++;
		goto Step;
	End:
		loopEnd = samples.Length / 4;
	}
	public static byte[] Convert (byte[] data, int from) {
		if(from == 24) {
			var r = new byte[data.Length * 2 / 3];
			var j = 0;
			for(int i = 0; i < data.Length; i += 3) {
				r[j] = data[i + 1];
				r[j + 1] = data[i + 2];
				j += 2;
			}
			return r;
		} else if(from == 32) {
			var r = new byte[data.Length / 2];
			for(int i = 0; i < data.Length; i += 4) {
				var fl = BitConverter.ToSingle(data[i..(i + 3)]);
				var val = BitConverter.GetBytes(fl * 32768);
				r[i / 2] = val[0];
				r[i / 2 + 1] = val[1];
			}
			return r;
		}
		return [];
	}
}
