using Microsoft.Practices.Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpTerminal
{
    public class LineEnding
    {
        public string Description { get; set; }
        public byte[] Data { get; set; }

        public override string ToString()
        {
            return Description;
        }
    }

    public class MainViewModel : BindableBase
    {
        public MainViewModel()
        {
            Serial = new Serial();
            SelectedLineEnding = lineendings.First();
        }
        Serial serial;

        public Serial Serial
        {
            get
            {
                return serial;
            }
            set
            {
                SetProperty(ref serial, value);
            }
        }

        ObservableCollection<LineEnding> lineendings = new ObservableCollection<LineEnding>(
                new LineEnding[] { 
                    new LineEnding(){Description = "CR", Data = new byte[]{ (byte)'\r'}},
                    new LineEnding(){Description = "CR LF", Data = new byte[]{ (byte)'\r', (byte)'\n'}},
                    new LineEnding(){Description = "LF CR", Data = new byte[]{ (byte)'\n', (byte)'\r'}},
                    new LineEnding(){Description = "LF", Data = new byte[]{ (byte)'\n'}},
                    new LineEnding(){Description = "None", Data = new byte[]{}},
                }
            );

        LineEnding selectedlineending;
        public LineEnding SelectedLineEnding
        {
            get
            {
                return selectedlineending;
            }
            set
            {
                SetProperty(ref selectedlineending, value);
            }
        }

        public ObservableCollection<LineEnding> LineEndings
        {
            get
            {
                return lineendings;
            }
            set
            {
                SetProperty(ref lineendings, value);
            }
        }
    }
}
