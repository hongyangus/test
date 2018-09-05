using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PropertyManagement.ViewModels.Communication
{
    public class CommunicationVM
    {
        public int senderID;
        public int recieverID;
        public string subject;
        public string content;
        public int emailTemplateID;
    }
}