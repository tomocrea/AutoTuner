using AutoTuner.GPU;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;

namespace AutoTuner
{
    public partial class UserInterface : Form
    {
        private IGpuTuning myTuning;
        private IGpuMonitoring myMonitor;
        private IGpuState myState;
        private CancellationTokenSource stopTuning;
        private ScottPlot.Plottables.DataLogger clockLogger;
        private ScottPlot.Plottables.DataLogger tempLogger;
        private ScottPlot.Plottables.DataLogger powerLogger;
        public UserInterface(IGpuTuning tuning, IGpuMonitoring monitoring, IGpuState state)
        {
            InitializeComponent();

            myTuning = tuning;
            myMonitor = monitoring;
            myState = state;

            Graphs();

            MessageBox.Show("Warning: Tuning can cause instability or damage to components, use at your own risk.");
        }

        private void Graphs()
        {
            formsPlot1.Plot.Axes.Left.Label.Text = "Clock Speed (MHz)";
            clockLogger = formsPlot1.Plot.Add.DataLogger();
            clockLogger.Color = Colors.Cyan;
            clockLogger.LineWidth = 3;

            formsPlot2.Plot.Axes.Left.Label.Text = "Hotspot Temp (°C)";
            tempLogger = formsPlot2.Plot.Add.DataLogger();
            tempLogger.Color = Colors.Red;
            tempLogger.LineWidth = 3;

            formsPlot3.Plot.Axes.Left.Label.Text = "Board Power (W)";
            powerLogger = formsPlot3.Plot.Add.DataLogger();
            powerLogger.Color = Colors.Green;
            powerLogger.LineWidth = 3;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                string selectedMode = comboBox1.SelectedItem.ToString();
                button1.Enabled = false;
                button2.Enabled = true;

                stopTuning = new CancellationTokenSource();
                Progress<UIUpdate> progress = new Progress<UIUpdate>(update =>
                {
                    if (update.Message != null && update.Message != "") label3.Text = update.Message;
                    if (update.Stage != null && update.Stage != "") label1.Text = "Current stage: " + update.Stage;

                    if (update.HotspotTemp > 0) tempLogger.Add(update.HotspotTemp);
                    if (update.ClockSpeed > 0) clockLogger.Add(update.ClockSpeed);
                    if (update.PowerUsage > 0) powerLogger.Add(update.PowerUsage);

                    if(update.GPUUsage > 0) label2.Text = "GPU Usage: " + update.GPUUsage + "%";

                    formsPlot1.Refresh();
                    formsPlot2.Refresh();
                    formsPlot3.Refresh();
                });

                try
                {
                    if (selectedMode == "Overclock (Increases power limit)")
                    {
                        TuningGpu tuner = new TuningGpu(myTuning, myState, TuningGpu.TuningTarget.overclock);
                        await tuner.TuningLoop(myTuning, myMonitor, progress, stopTuning.Token);
                    }
                    else if (selectedMode == "Undervolt (Decreases power limit)")
                    {
                        TuningGpu tuner = new TuningGpu(myTuning, myState, TuningGpu.TuningTarget.undervolt);
                        await tuner.TuningLoop(myTuning, myMonitor, progress, stopTuning.Token);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred during tuning: {ex.Message} Please try again.");
                }
                finally
                {
                    button1.Enabled = true;
                }
            }
            else
            {
                MessageBox.Show("Please select a GPU from the dropdown.");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(stopTuning != null && !stopTuning.IsCancellationRequested)
            {
                stopTuning.Cancel();
                button2.Enabled = false;
                button1.Enabled = true;
            }
        }
    }
}
