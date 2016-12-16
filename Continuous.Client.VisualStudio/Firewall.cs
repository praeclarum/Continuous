using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetFwTypeLib;

namespace Continuous.Client.VisualStudio
{
    public static class Firewall
    {
        public static void AddUdpInRuleIfNeeded(int port, string name)
        {
            INetFwPolicy2 policy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            
            var enabled = policy.FirewallEnabled[(NET_FW_PROFILE_TYPE2_)policy.CurrentProfileTypes];

            if (!enabled) return;

            INetFwRule rule = policy.Rules.OfType<INetFwRule>().Where(x => x.Name == name).FirstOrDefault();

            if (rule == null)
            {
                var args = $"advfirewall firewall add rule name=\"{name}\" dir=in action=allow protocol=UDP localport={port}";
                var si = new ProcessStartInfo("netsh", args);
                si.Verb = "runas";
                var p = Process.Start(si);

                /*
                rule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                rule.Name = name;
                rule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP;
                rule.RemotePorts = port.ToString();
                rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
                rule.Enabled = true;
                rule.Profiles = policy.CurrentProfileTypes;
                rule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                policy.Rules.Add(rule);*/
            }
        }
    }
}
