using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public sealed class ArenaAnnouncer : MonoBehaviour
{
    const string EnabledKey = "ArenaAnnouncerEnabled";
    const string VolumeKey = "ArenaAnnouncerVolume";
    public const string IntroLine = "Arena Chaos başlıyor! Hazır mısınız?";
    const int QueueLimit = 4;

    static ArenaAnnouncer instance;
    readonly Queue<string> pending = new Queue<string>();
    AudioSource voice;
    AudioClip introClip;
    Task<Speech> synthesis;
    float voiceEnds;
    float duck = 1f;
    string status = "Hazır";
    WindowsVoice backend;
    Task warmup;
    bool discardSynthesis;

    public static float BackgroundGain => instance != null ? instance.duck : 1f;
    public string Status => status;
    public bool Enabled => PlayerPrefs.GetInt(EnabledKey, 1) != 0;
    public float Volume => PlayerPrefs.GetFloat(VolumeKey, .92f);

    public static ArenaAnnouncer GetOrCreate()
    {
        if (instance == null)
        {
            var obj = new GameObject("Arena announcer");
            instance = obj.AddComponent<ArenaAnnouncer>();
            DontDestroyOnLoad(obj);
        }
        return instance;
    }

    void Awake()
    {
        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.spatialBlend = 0f;
        voice.priority = 0;
        voice.ignoreListenerPause = true;

        introClip = Resources.Load<AudioClip>("ArenaChaos/Announcer/Intro");
        var engine = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
        var runtime = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ArenaChaos", "AnnouncerVoice");
        var python = Path.Combine(runtime, "voice-convert", "Scripts", "python.exe");
        var script = Path.Combine(Application.streamingAssetsPath, "ArenaVoiceWorker.py");
        if (!File.Exists(engine) || !File.Exists(python) || !File.Exists(script))
        {
            status = "Referans spiker kurulumu eksik";
            return;
        }
        backend = new WindowsVoice(engine, python, script, runtime);
        if (Enabled) WarmVoice();
    }

    public void SetEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(EnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        if (enabled)
        {
            WarmVoice();
            return;
        }

        pending.Clear();
        discardSynthesis = synthesis != null;
        voice.Stop();
        voiceEnds = 0f;
    }

    public void SetVolume(float volume)
    {
        PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(volume));
        if (voice != null) voice.volume = Volume;
    }

    public static void PlayerEntered(ArenaFighter fighter)
    {
        if (fighter == null || fighter.IsBot || string.IsNullOrEmpty(fighter.ViewerId)) return;
        GetOrCreate().Enqueue(EntryLine(fighter.DisplayName));
    }

    public static void DuelStarted(string stage, string left, string right) =>
        GetOrCreate().Enqueue(DuelLine(stage, left, right), priority: true);

    public static void ChampionWon(string name) =>
        GetOrCreate().Enqueue(ChampionLine(name), priority: true);

    public static string EntryLine(string name) => $"{SpokenName(name)} arenada!";
    public static string DuelLine(string stage, string left, string right) =>
        $"{stage}. {SpokenName(left)} ve {SpokenName(right)}. Dövüş!";
    public static string ChampionLine(string name) => $"Arenanın şampiyonu. {SpokenName(name)}!";

    static string SpokenName(string value)
    {
        var clean = new StringBuilder();
        foreach (char ch in (value ?? ""))
        {
            if (clean.Length >= 28) break;
            if (char.IsLetterOrDigit(ch)) clean.Append(ch);
            else if ((ch == ' ' || ch == '_' || ch == '.' || ch == '-') && clean.Length > 0 && clean[clean.Length - 1] != ' ')
                clean.Append(' ');
        }
        return clean.Length > 0 ? clean.ToString().Trim() : "Savaşçı";
    }

    public void Preview(string line) => Enqueue(line, priority: true);

    void Enqueue(string line, bool priority = false)
    {
        if (!Enabled) return;
        if (priority)
        {
            pending.Clear();
            discardSynthesis = synthesis != null;
        }
        if (pending.Count < QueueLimit) pending.Enqueue(line);
    }

    void WarmVoice()
    {
        if (backend == null || warmup != null) return;
        status = "Türkçe spiker hazırlanıyor…";
        warmup = Task.Run(backend.Warm);
    }

    void Update()
    {
        if (warmup != null && warmup.IsCompleted)
        {
            status = warmup.IsFaulted ? warmup.Exception.GetBaseException().Message : "Türkçe spiker hazır";
            warmup = null;
        }

        float now = Time.unscaledTime;
        bool speaking = now < voiceEnds;
        duck = Mathf.MoveTowards(duck, speaking ? .22f : 1f,
            Time.unscaledDeltaTime * (speaking ? 12f : 1.4f));

        if (synthesis != null && synthesis.IsCompleted && !speaking)
        {
            var completed = synthesis;
            synthesis = null;
            if (completed.IsFaulted)
            {
                if (!discardSynthesis) status = completed.Exception.GetBaseException().Message;
            }
            else if (Enabled && !discardSynthesis) Play(completed.Result, now);
            discardSynthesis = false;
        }

        if (speaking || synthesis != null || !Enabled || pending.Count == 0) return;
        if (pending.Peek() == IntroLine && introClip != null)
        {
            pending.Dequeue();
            PlayClip(introClip, now);
            return;
        }
        if (warmup != null) return;
        if (backend == null)
        {
            pending.Clear();
            return;
        }

        string line = pending.Dequeue();
        status = "Anons hazırlanıyor…";
        synthesis = Task.Run(() => backend.Synthesize(line));
    }

    void Play(Speech speech, float now)
    {
        var clip = AudioClip.Create("Arena announcer", speech.Samples.Length, 1, speech.Rate, false);
        clip.SetData(speech.Samples, 0);
        PlayClip(clip, now);
    }

    void PlayClip(AudioClip clip, float now)
    {
        if (voice.clip != null && voice.clip != introClip) Destroy(voice.clip);
        voice.clip = clip;
        voice.volume = Volume;
        const float leadIn = .09f;
        voice.PlayScheduled(AudioSettings.dspTime + leadIn);
        voiceEnds = now + leadIn + clip.length;
        duck = .22f;
        status = "Türkçe spiker hazır";
    }

    sealed class Speech
    {
        public int Rate;
        public float[] Samples;
    }

    // The worker reads and decodes plain data, separate from the fixed speech command.
    const string VoiceCommand = @"
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
try {
    Add-Type -AssemblyName System.Runtime.WindowsRuntime
    $null = [Windows.Media.SpeechSynthesis.SpeechSynthesizer,Windows.Media.SpeechSynthesis,ContentType=WindowsRuntime]
    $null = [Windows.Media.SpeechSynthesis.SpeechSynthesisStream,Windows.Media.SpeechSynthesis,ContentType=WindowsRuntime]
    $voice = [Windows.Media.SpeechSynthesis.SpeechSynthesizer]::AllVoices |
        Where-Object Language -like 'tr-*' |
        Sort-Object @{Expression={if($_.Gender -eq 'Male'){0}else{1}}} |
        Select-Object -First 1
    if ($null -eq $voice) { throw 'Windows Türkçe konuşma sesi yüklü değil.' }
    $asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
        $_.Name -eq 'AsTask' -and $_.IsGenericMethodDefinition -and
        $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'
    } | Select-Object -First 1
    $engine = New-Object Windows.Media.SpeechSynthesis.SpeechSynthesizer
    try {
        $engine.Voice = $voice
        $engine.Options.SpeakingRate = 1.20
        [Console]::WriteLine('READY')
        while ($null -ne ($line = [Console]::ReadLine())) {
            try {
                $parts = $line.Split([char]9)
                $text = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($parts[0]))
                $path = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($parts[1]))
                $operation = $engine.SynthesizeTextToStreamAsync($text)
                $task = $asTask.MakeGenericMethod([Windows.Media.SpeechSynthesis.SpeechSynthesisStream]).Invoke($null, @($operation))
                $task.Wait()
                $wave = [System.IO.WindowsRuntimeStreamExtensions]::AsStreamForRead($task.Result)
                try {
                    $file = [IO.File]::Create($path)
                    try { $wave.CopyTo($file) } finally { $file.Dispose() }
                } finally { $wave.Dispose() }
                [Console]::WriteLine('OK')
            } catch {
                [Console]::WriteLine('ERR:' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($_.Exception.Message)))
            }
        }
    } finally { $engine.Dispose() }
} catch {
    [Console]::WriteLine('ERR:' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($_.Exception.Message)))
    exit 1
}";

    sealed class WindowsVoice : IDisposable
    {
        readonly string executable;
        readonly string python;
        readonly string script;
        readonly string runtime;
        readonly object workerLock = new object();
        Process worker;
        Process converter;

        public WindowsVoice(string executable, string python, string script, string runtime)
        {
            this.executable = executable;
            this.python = python;
            this.script = script;
            this.runtime = runtime;
        }

        public void Warm()
        {
            lock (workerLock) EnsureRunning();
        }

        public Speech Synthesize(string line)
        {
            string file = Path.Combine(Path.GetTempPath(), "arena-voice-" + Guid.NewGuid().ToString("N") + ".wav");
            lock (workerLock)
            {
                try
                {
                    EnsureRunning();
                    worker.StandardInput.WriteLine(Encode(line) + "\t" + Encode(file));
                    worker.StandardInput.Flush();
                    Expect(worker, "OK", 15000);
                    converter.StandardInput.WriteLine(Encode(file));
                    converter.StandardInput.Flush();
                    Expect(converter, "OK", 30000);
                    using (var wave = File.OpenRead(file)) return ReadWave(wave);
                }
                catch
                {
                    Dispose();
                    throw;
                }
                finally
                {
                    if (File.Exists(file)) File.Delete(file);
                }
            }
        }

        void EnsureRunning()
        {
            if (worker != null && !worker.HasExited && converter != null && !converter.HasExited) return;
            Dispose();
            worker = Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                Arguments = "-NoLogo -NoProfile -NonInteractive -EncodedCommand " +
                    Convert.ToBase64String(Encoding.Unicode.GetBytes(VoiceCommand)),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            });
            try
            {
                converter = Process.Start(new ProcessStartInfo
                {
                    FileName = python,
                    Arguments = "-u \"" + script + "\" \"" + runtime + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                });
                Expect(worker, "READY", 45000);
                Expect(converter, "READY", 120000);
            }
            catch { Dispose(); throw; }
        }

        static void Expect(Process process, string expected, int timeout)
        {
            var response = process.StandardOutput.ReadLineAsync();
            if (!response.Wait(timeout))
                throw new InvalidOperationException("Windows spikeri zaman aşımına uğradı.");

            string message = response.Result;
            if (message == expected) return;
            if (message != null && message.StartsWith("ERR:", StringComparison.Ordinal))
                throw new InvalidOperationException(Encoding.UTF8.GetString(Convert.FromBase64String(message.Substring(4))));
            throw new InvalidOperationException("Windows spikeri yanıt vermedi.");
        }

        static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

        public void Dispose()
        {
            Stop(ref converter);
            Stop(ref worker);
        }

        static void Stop(ref Process process)
        {
            if (process == null) return;
            try
            {
                if (!process.HasExited) process.Kill();
            }
            catch (InvalidOperationException) { }
            finally
            {
                process.Dispose();
                process = null;
            }
        }

        public void Close()
        {
            lock (workerLock) Dispose();
        }
    }

    static Speech ReadWave(Stream stream)
    {
        using (var reader = new BinaryReader(stream))
        {
            if (reader.ReadUInt32() != 0x46464952) throw new InvalidDataException("Spiker ses dosyası okunamadı.");
            reader.ReadUInt32();
            if (reader.ReadUInt32() != 0x45564157) throw new InvalidDataException("Spiker ses dosyası okunamadı.");
            int rate = 0;
            while (stream.Position + 8 <= stream.Length)
            {
                uint tag = reader.ReadUInt32();
                uint size = reader.ReadUInt32();
                long end = stream.Position + size;
                if (end > stream.Length) break;

                if (tag == 0x20746D66 && size >= 16)
                {
                    int format = reader.ReadUInt16(), channels = reader.ReadUInt16();
                    rate = reader.ReadInt32();
                    reader.ReadUInt32();
                    reader.ReadUInt16();
                    int bits = reader.ReadUInt16();
                    if (format != 1 || channels != 1 || bits != 16 || rate < 8000 || rate > 48000) break;
                }
                else if (tag == 0x61746164 && rate > 0 && size >= 2 && size <= rate * 30 * 2)
                {
                    var samples = new float[size / 2];
                    for (int i = 0; i < samples.Length; i++)
                        samples[i] = reader.ReadInt16() / 32768f;

                    return new Speech { Rate = rate, Samples = samples };
                }
                stream.Position = end + (size & 1);
            }
        }
        throw new InvalidDataException("Spiker ses biçimi desteklenmiyor.");
    }

    void OnDestroy()
    {
        if (backend != null) _ = Task.Run(backend.Close);
        if (voice != null && voice.clip != null && voice.clip != introClip) Destroy(voice.clip);
        if (instance == this) instance = null;
    }
}
