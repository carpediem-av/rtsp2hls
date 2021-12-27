/**********************************************************************/
/* Copyright (c) 2021 Carpe Diem Software Developing by Alex Versetty */
/* http://carpediem.0fees.us                                          */
/**********************************************************************/

using System.Windows.Forms;

namespace CDSD.Forms
{
	public partial class FAbout : Form
	{
		public FAbout()
		{
			InitializeComponent();
		}

		private void www_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://carpediem.0fees.us");
		}
	}
}
