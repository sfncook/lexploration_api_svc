using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

// azureVisemeIdToModelCodes = {
//     0: {"target": "viseme_sil", "value": 1},
//     1: {"target": "viseme_aa", "value": 1},
//     2: {"target": "viseme_aa", "value": 1},
//     3: {"target": "viseme_O", "value": 1},
//     4: {"target": "viseme_O", "value": 0.7},
//     5: {"target": "viseme_RR", "value": 1},
//     6: {"target": "viseme_I", "value": 0.7},
//     7: {"target": "viseme_U", "value": 1},
//     8: {"target": "viseme_aa", "value": 0.8},
//     9: {"target": "viseme_O", "value": 1},
//     10: {"target": "viseme_aa", "value": 0.7},
//     11: {"target": "viseme_aa", "value": 1},
//     12: {"target": "viseme_RR", "value": 0.8},
//     13: {"target": "viseme_O", "value": 1},
//     14: {"target": "viseme_O", "value": 1},
//     15: {"target": "viseme_SS", "value": 1},
//     16: {"target": "viseme_CH", "value": 1},
//     17: {"target": "viseme_TH", "value": 1},
//     18: {"target": "viseme_FF", "value": 1},
//     19: {"target": "viseme_DD", "value": 1},
//     20: {"target": "viseme_kk", "value": 1},
//     21: {"target": "viseme_PP", "value": 1},
// }
public class AzureSpeechService
{
    static string speechKey = "82af17a42be449a7a74300542d1a4938";
    static string speechRegion = "westus3";

    static Dictionary<uint, VisemeTargetValue> visemeCodes = new Dictionary<uint, VisemeTargetValue>
    {
        {0, new VisemeTargetValue { Target = "viseme_sil", Value = 1 }},
        {1, new VisemeTargetValue { Target = "viseme_aa", Value = 1 }},
        {2, new VisemeTargetValue { Target = "viseme_aa", Value = 1 }},
        {3, new VisemeTargetValue { Target = "viseme_O", Value =  1}},
        {4, new VisemeTargetValue { Target = "viseme_O", Value =  0.7F}},
        {5, new VisemeTargetValue { Target = "viseme_RR", Value =  1}},
        {6, new VisemeTargetValue { Target = "viseme_I", Value =  0.7F}},
        {7, new VisemeTargetValue { Target = "viseme_U", Value =  1}},
        {8, new VisemeTargetValue { Target = "viseme_aa", Value =  0.8F}},
        {9, new VisemeTargetValue { Target = "viseme_O", Value =  1}},
        {10, new VisemeTargetValue { Target = "viseme_aa", Value =  0.7F}},
        {11, new VisemeTargetValue { Target = "viseme_aa", Value =  1}},
        {12, new VisemeTargetValue { Target = "viseme_RR", Value =  0.8F}},
        {13, new VisemeTargetValue { Target = "viseme_O", Value =  1}},
        {14, new VisemeTargetValue { Target = "viseme_O", Value =  1}},
        {15, new VisemeTargetValue { Target = "viseme_SS", Value =  1}},
        {16, new VisemeTargetValue { Target = "viseme_CH", Value =  1}},
        {17, new VisemeTargetValue { Target = "viseme_TH", Value =  1}},
        {18, new VisemeTargetValue { Target = "viseme_FF", Value =  1}},
        {19, new VisemeTargetValue { Target = "viseme_DD", Value =  1}},
        {20, new VisemeTargetValue { Target = "viseme_kk", Value =  1}},
        {21, new VisemeTargetValue { Target = "viseme_PP", Value =  1}},
    };

    public AzureSpeechService()
    {
    }

    public async Task<SpeechResults> GetSpeech(string text) {
        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm);


        string tempPath = Path.GetTempPath();
        string randomFileName = Path.ChangeExtension(Path.GetRandomFileName(), ".wav");
        string fullPath = Path.Combine(tempPath, randomFileName);
        Console.WriteLine($"fullPath: {fullPath}");
        using var audioConfig = AudioConfig.FromWavFileOutput(fullPath);


        // The language of the voice that speaks.
        speechConfig.SpeechSynthesisVoiceName = "en-US-JennyNeural"; 

        List<VisemeData> visemeData = new List<VisemeData>();
        using (var speechSynthesizer = new SpeechSynthesizer(speechConfig, audioConfig))
        {
            speechSynthesizer.VisemeReceived += (s, e) =>
            {
                Console.WriteLine($"Viseme event received. Audio offset: " +
                    $"{e.AudioOffset / 10000}ms, viseme id: {e.VisemeId}.");


                visemeData.Add(new VisemeData{
                    offset = e.AudioOffset / 10000000.0,
                    visemeId = e.VisemeId,
                    target = visemeCodes[e.VisemeId].Target,
                    value = visemeCodes[e.VisemeId].Value
                });
            };
            await speechSynthesizer.SpeakTextAsync(text);
        }

        LipSyncResults lipSyncResults = new LipSyncResults();
        double prev_offset = 0;
        foreach(VisemeData vd in visemeData) {
            LipSyncData lipSyncData = new LipSyncData() {
                start = prev_offset,
                end = vd.offset,
                target = vd.target,
                value = vd.value,
            };
            prev_offset = vd.offset;
            lipSyncResults.AddMouthCue(lipSyncData);
        }

        SpeechResults speechResults = new SpeechResults() {
            lipsync = lipSyncResults,
            audio = await AudioFileToBase64Async(fullPath)
        };
        return speechResults;
    }

    private async Task<string> AudioFileToBase64Async(string filePath)
    {
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            byte[] bytes = new byte[fileStream.Length];
            await fileStream.ReadAsync(bytes, 0, bytes.Length);
            return Convert.ToBase64String(bytes);
        }
    }

    public class VisemeData {
        public double offset { get; set;}
        public uint visemeId { get; set;}
        public string target { get; set;}
        public float value { get; set;}
    }

    public class VisemeTargetValue
    {
        public string Target { get; set; }
        public float Value { get; set; }
    }

    public class LipSyncData
    {
        public double start { get; set; }
        public double end { get; set; }
        public string target { get; set; }
        public float value { get; set; }
    }
    public class LipSyncResults
    {
        public List<LipSyncData> mouthCues { get; set; }

        public LipSyncResults()
        {
            mouthCues = new List<LipSyncData>();
        }

        public void AddMouthCue(LipSyncData lipSyncData)
        {
            mouthCues.Add(lipSyncData);
        }
    }
    public class SpeechResults
    {
        public LipSyncResults lipsync { get; set; }
        public string audio { get; set; }
    }
}
