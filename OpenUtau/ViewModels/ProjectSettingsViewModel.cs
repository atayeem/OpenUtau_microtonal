using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using ReactiveUI.Fody.Helpers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System;

namespace OpenUtau.App.ViewModels {
    public class ProjectSettingsViewModel : ViewModelBase {
        [Reactive] public int EqualTemperament { get; set; }
        [Reactive] public double ConcertPitch { get; set; }
        [Reactive] public int ConcertPitchNote { get; set; }
        public double[]? ToneToFreqMap { get; set; }

        private UProject project;

        public ProjectSettingsViewModel(UProject project) {
            this.project = project;
            EqualTemperament = project.EqualTemperament;
            ConcertPitch = project.ConcertPitch;
            ConcertPitchNote = project.ConcertPitchNote;
            ToneToFreqMap = project.ToneToFreqMap;
        }

        public void Apply() {
            if (project.EqualTemperament != EqualTemperament ||
                project.ConcertPitch != ConcertPitch ||
                project.ConcertPitchNote != ConcertPitchNote ||
                ToneToFreqMap != null) {
                DocManager.Inst.StartUndoGroup();
                DocManager.Inst.ExecuteCmd(new ConfigureProjectCommand(
                    project,
                    newEqualTemperament: EqualTemperament,
                    newConcertPitch: ConcertPitch,
                    newConcertPitchNote: ConcertPitchNote,
                    newToneToFreqMap: ToneToFreqMap));
                DocManager.Inst.EndUndoGroup();
            }
        }

        public void LoadTun(string path) {
            try {
                var cents = new double[128];
                for (int i = 0; i < 128; i++) {
                    cents[i] = i * 100;
                }
                double baseFreq = -1;
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines) {
                    if (line.StartsWith(";") || string.IsNullOrWhiteSpace(line)) {
                        continue;
                    }
                    if (line.Contains("=")) {
                        var parts = line.Split('=');
                        if (parts.Length == 2) {
                            var key = parts[0].Trim();
                            var value = parts[1].Trim();
                            if (key.StartsWith("note")) {
                                if (int.TryParse(key.Substring(4), out int noteNum)) {
                                    if (noteNum >= 0 && noteNum < 128) {
                                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double centsValue)) {
                                            cents[noteNum] = centsValue;
                                        }
                                    }
                                }
                            } else if (key == "basefreq") {
                                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double freq)) {
                                    baseFreq = freq;
                                }
                            }
                        }
                    }
                }

                var map = new double[128];
                if (baseFreq < 0) {
                    ConcertPitch = 440.0;
                    ConcertPitchNote = 69;
                    baseFreq = ConcertPitch / Math.Pow(2, cents[ConcertPitchNote] / 1200.0);
                } else {
                    ConcertPitch = baseFreq * Math.Pow(2, cents[69] / 1200.0);
                    ConcertPitchNote = 69;
                }

                for (int i = 0; i < 128; i++) {
                    map[i] = baseFreq * Math.Pow(2, cents[i] / 1200.0);
                }
                ToneToFreqMap = map;
                EqualTemperament = 0; // Indicate custom tuning
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(e));
            }
        }
    }

    public class ConfigureProjectCommand : UCommand {
        private readonly UProject project;
        private readonly int newEqualTemperament;
        private readonly double newConcertPitch;
        private readonly int newConcertPitchNote;
        private readonly double[]? newToneToFreqMap;
        private readonly int oldEqualTemperament;
        private readonly double oldConcertPitch;
        private readonly int oldConcertPitchNote;
        private readonly double[]? oldToneToFreqMap;

        public ConfigureProjectCommand(UProject project, int newEqualTemperament, double newConcertPitch, int newConcertPitchNote, double[]? newToneToFreqMap) {
            this.project = project;
            this.newEqualTemperament = newEqualTemperament;
            this.newConcertPitch = newConcertPitch;
            this.newConcertPitchNote = newConcertPitchNote;
            this.newToneToFreqMap = newToneToFreqMap;
            this.oldEqualTemperament = project.EqualTemperament;
            this.oldConcertPitch = project.ConcertPitch;
            this.oldConcertPitchNote = project.ConcertPitchNote;
            this.oldToneToFreqMap = project.ToneToFreqMap;
        }

        public override string ToString() => "Configure project";
        public override void Execute() {
            project.EqualTemperament = newEqualTemperament;
            project.ConcertPitch = newConcertPitch;
            project.ConcertPitchNote = newConcertPitchNote;
            if (newToneToFreqMap != null) {
                project.SetToneToFreqMap(newToneToFreqMap);
            } else {
                project.UpdateToneToFreqMap();
            }
        }
        public override void Unexecute() {
            project.EqualTemperament = oldEqualTemperament;
            project.ConcertPitch = oldConcertPitch;
            project.ConcertPitchNote = oldConcertPitchNote;
            if (oldToneToFreqMap != null) {
                project.SetToneToFreqMap(oldToneToFreqMap);
            } else {
                project.UpdateToneToFreqMap();
            }
        }
    }
}
