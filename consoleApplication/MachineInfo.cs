using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace consoleApplication
{
    class MachineInfo
    {

        public int Id { get; set; }



        public string MachineId { get; set; }



        public string Manufacturer { get; set; }



        public string MachineName { get; set; }



        public string MachineType { get; set; }



        public string CommunicationInterface { get; set; }



        public string IsProductionFileRequired { get; set; }



        public string IsMaterialRequired { get; set; }



        public string ValidFileName { get; set; }



        public string ValidFileExtension { get; set; }



        public string TcpServerAddress { get; set; }

        public List<Instruction> instructionList { get; set; }

        public List<SetupAssistent> setupAssistentList { get; set; }
    }
}
