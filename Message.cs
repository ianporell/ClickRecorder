using System.Windows.Forms;

namespace ClickRecorder
{
    public partial class Message : Form
    {
        public Message(string title1 = null, string title2 = null, string[] buttons = null)
        {
            InitializeComponent();
            label1.Text = title1;
            label2.Text = title2;

            if (buttons != null)
            {
                int left = 0;
                foreach(string buttonText in buttons)
                {
                    Button button = new Button();
                    button.Text = buttonText;
                    button.Left = left;
                    left += 50;
                }
            }
        }
    }
}
