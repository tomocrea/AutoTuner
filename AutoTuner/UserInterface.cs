using AutoTuner.GPU;
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
        public UserInterface(IGpuTuning tuning, IGpuMonitoring monitoring, IGpuState state)
        {
            InitializeComponent();

            myTuning = tuning;
            myMonitor = monitoring;
            myState = state;

            MessageBox.Show("Warning: Tuning can cause instability or damage to components, use at your own risk.");
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if(comboBox1.SelectedItem != null)
            {
                string selectedMode = comboBox1.SelectedItem.ToString();
                button1.Enabled = false;

                try
                {
                    if(selectedMode == "Overclock (Increases power limit)")
                    {
                        TuningGpu tuner = new TuningGpu(myTuning, myState, TuningGpu.TuningTarget.overclock);
                        await tuner.TuningLoop(myTuning, myMonitor);
                    }
                    else if(selectedMode == "Undervolt (Decreases power limit)")
                    {
                        TuningGpu tuner = new TuningGpu(myTuning, myState, TuningGpu.TuningTarget.undervolt);
                        await tuner.TuningLoop(myTuning, myMonitor);
                    }
                }
                catch(Exception ex)
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
    }
}
