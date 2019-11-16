using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace sdxs.Controllers
{
    [ApiController]
    [Route("")]
    public class SdxsController : ControllerBase
    {
        private readonly ILogger<SdxsController> _logger;

        public SdxsController(ILogger<SdxsController> logger)
        {
            _logger = logger;
        }

        [HttpGet()]
        public IEnumerable<dynamic> Get()
        {    
            //repurpose this action for signaling

            //for now, print headers        
            foreach (var hdr in HttpContext.Request.Headers)
            {
                yield return hdr.Key + " : " + hdr.Value;
            } 
        }

        [HttpPost("seed")]
        public void Seed()
        {
            if (GuardApiKey()) return;
            if (GuardEphemeral()) return;
            if (GuardDomain(checkWhitelist:true)) return;

            //send nodes.json payload to seed requestor

            SetResponseAccepted();
            return; 
        }

        [HttpPost("sync")]
        public void SyncNodes([FromBody] string syn)
        {
            if (GuardSyn(syn)) return;
            if (GuardEphemeral()) return;
            if (GuardApiKey()) return;

            //guard hash not same
            //guard date greater
            //wholesale replace data set

            SetResponseAccepted();
            return; 
        }

        [HttpPost("msin")]
        public void InboundMsg([FromBody] string msg)
        {
            if (GuardMsg(msg)) return;
            if (GuardEphemeral()) return;
            if (GuardApiKey()) return;
            if (GuardDomain(checkWhitelist:true)) return;

            dynamic m = new ExpandoObject();
            m.from = HttpContext.Request.Headers["Host"];
            m.msg = msg;
            
            File.WriteAllText($"Messages/{DateTime.UtcNow.Ticks}.json", JsonConvert.SerializeObject(m));

            SetResponseAccepted();
            return; 
        }

        [HttpPost("msbr")]
        public void BroadcastMsg([FromBody] string msg)
        {
            if (GuardMsg(msg)) return;
            if (GuardAction()) return;

            //validate msg
            //transmit message via gossip

            SetResponseAccepted();
            return; 
        }
                
        [HttpPost("msdi")]
        public void SendDirectConnectMsg([FromBody] string msg)
        {
            if (GuardDomain()) return;
            if (GuardAction()) return;
            if (GuardMsg(msg)) return;

            //transmit message to destination domain

            SetResponseAccepted();
            return; 
        }

        [HttpPost("msch")]
        public void SendChannelMsg([FromBody] string msg)
        {
            if (GuardChannel()) return;
            if (GuardAction()) return;
            if (GuardMsg(msg)) return;

            //transmit message to channel recipients

            SetResponseAccepted();
            return; 
        }

        /////

        private bool GuardEphemeral()
        {
            var ephemval = HttpContext.Request.Headers[SDXS_GLOBALS.C_SDXS_HDRKEY_EPHEMERAL_VAL];
            var ephemsig = HttpContext.Request.Headers[SDXS_GLOBALS.C_SDXS_HDRKEY_EPHEMERAL_SIG];
            
            if (string.IsNullOrEmpty(ephemval)
                || string.IsNullOrEmpty(ephemsig)) {
                SetResponseBadRequest();
                return true; 
            }

            //todo: check ephemeral value within +-5mins of UTC Now.
            //todo: check ephemeral signature against requestor sdxs-ident

            return false;
        }

        private bool GuardApiKey()
        {
            var apikey = HttpContext.Request.Headers[SDXS_GLOBALS.C_SDXS_HDRKEY_IDENT];
            
            if (string.IsNullOrEmpty(apikey)) {
                SetResponseBadRequest();
                return true; 
            }

            return false;
        }

        private bool GuardDomain(bool checkWhitelist = false)
        {
            var domain = HttpContext.Request.Headers[SDXS_GLOBALS.C_SDXS_HDRKEY_DOMAIN];
            
            if (string.IsNullOrEmpty(domain)) {
                SetResponseBadRequest();
                return true; 
            }

            return false;
        }

        private bool GuardSyn(string syn)
        {            
            if (string.IsNullOrEmpty(syn)) {
                SetResponseBadRequest();
                return true; 
            }

            return false;
        }

        private bool GuardMsg(string msg)
        {            
            if (string.IsNullOrEmpty(msg)) {
                SetResponseBadRequest();
                return true; 
            }

            return false;
        }

        private bool GuardChannel()
        {            
            var channel = HttpContext.Request.Headers[SDXS_GLOBALS.C_SDXS_HDRKEY_CHANNEL];
            
            if (string.IsNullOrEmpty(channel)) {
                SetResponseBadRequest();
                return true; 
            }

            return false;
        }

        private bool GuardAction()
        {            
            var action = HttpContext.Request.Headers[SDXS_GLOBALS.C_SDXS_HDRKEY_ACTION];
            
            if (string.IsNullOrEmpty(action)) {
                SetResponseBadRequest();
                return true; 
            }

            return false;
        }

        private void SetResponseAccepted()
        {
            HttpContext.Response.StatusCode = 202;
        }

        private void SetResponseBadRequest()
        {
            HttpContext.Response.StatusCode = 400;
        }
    }
}
