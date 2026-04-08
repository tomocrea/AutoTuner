using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoTuner.GPU
{
    //https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to
    internal class JsonGpuState : IGpuState
    {
        private string filePath = "gpu_state.json";
        public TuningState LoadState()
        {
            try
            {
                string json = System.IO.File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<TuningState>(json) ?? new TuningState { Status = TuningState.TuningStatus.Idle };
            }
            catch (Exception)
            {
                return new TuningState { Status = TuningState.TuningStatus.Idle };
            }
        }
        public async Task SaveState(TuningState state)
        {
            await using FileStream createStream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(createStream, state);
            //https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream.flush?view=net-10.0#system-io-filestream-flush(system-boolean)
            createStream.Flush(true);
            //System.IO.File.WriteAllText(filePath, json);
        }
        public void ClearState()
        {
            System.IO.File.Delete(filePath);
        }
    }
}
