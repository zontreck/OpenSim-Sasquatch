using System;
using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.ScriptBase;

namespace OpenSim.Region.ScriptEngine.Shared.Api.LSL
{
    public partial class LSL_Api: MarshalByRefObject, ILSL_Api, IScriptApi
    {
        
        public void llSetContentType(LSL_Key reqid, LSL_Integer type)
        {

            if (m_UrlModule == null)
                return;

            if(!UUID.TryParse(reqid, out UUID id) || id.IsZero())
                return;

            // Make sure the content type is text/plain to start with
            m_UrlModule.HttpContentType(id, "text/plain");

            // Is the object owner online and in the region
            ScenePresence agent = World.GetScenePresence(m_host.ParentGroup.OwnerID);
            if (agent == null || agent.IsChildAgent || agent.IsDeleted)
                return;  // Fail if the owner is not in the same region

            // Is it the embeded browser?
            string userAgent = m_UrlModule.GetHttpHeader(id, "user-agent");
            if(string.IsNullOrEmpty(userAgent))
                return;

            if (userAgent.IndexOf("SecondLife") < 0)
                return; // Not the embedded browser

            // Use the IP address of the client and check against the request
            // seperate logins from the same IP will allow all of them to get non-text/plain as long
            // as the owner is in the region. Same as SL!
            string logonFromIPAddress = agent.ControllingClient.RemoteEndPoint.Address.ToString();
            if (string.IsNullOrEmpty(logonFromIPAddress))
                return;

            string requestFromIPAddress = m_UrlModule.GetHttpHeader(id, "x-remote-ip");
            //m_log.Debug("IP from header='" + requestFromIPAddress + "' IP from endpoint='" + logonFromIPAddress + "'");
            if (requestFromIPAddress == null)
                return;

            requestFromIPAddress = requestFromIPAddress.Trim();

            // If the request isnt from the same IP address then the request cannot be from the owner
            if (!requestFromIPAddress.Equals(logonFromIPAddress))
                return;

            switch (type)
            {
                case ScriptBaseClass.CONTENT_TYPE_HTML:
                    m_UrlModule.HttpContentType(id, "text/html");
                    break;
                case ScriptBaseClass.CONTENT_TYPE_XML:
                    m_UrlModule.HttpContentType(id, "application/xml");
                    break;
                case ScriptBaseClass.CONTENT_TYPE_XHTML:
                    m_UrlModule.HttpContentType(id, "application/xhtml+xml");
                    break;
                case ScriptBaseClass.CONTENT_TYPE_ATOM:
                    m_UrlModule.HttpContentType(id, "application/atom+xml");
                    break;
                case ScriptBaseClass.CONTENT_TYPE_JSON:
                    m_UrlModule.HttpContentType(id, "application/json");
                    break;
                case ScriptBaseClass.CONTENT_TYPE_LLSD:
                    m_UrlModule.HttpContentType(id, "application/llsd+xml");
                    break;
                case ScriptBaseClass.CONTENT_TYPE_FORM:
                    m_UrlModule.HttpContentType(id, "application/x-www-form-urlencoded");
                    break;
                case ScriptBaseClass.CONTENT_TYPE_RSS:
                    m_UrlModule.HttpContentType(id, "application/rss+xml");
                    break;
                default:
                    m_UrlModule.HttpContentType(id, "text/plain");
                    break;
            }
        }
    }
}