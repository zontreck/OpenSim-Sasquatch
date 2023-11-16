using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Rendering;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.PhysicsModules.SharedBase;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.ScriptBase;
using OpenSim.Services.Interfaces;
using PresenceInfo = OpenSim.Region.Framework.Interfaces.PresenceInfo;

namespace OpenSim.Region.ScriptEngine.Shared.Api.LSL
{
    public partial class LSL_Api: MarshalByRefObject, ILSL_Api, IScriptApi
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private int m_llRequestAgentDataCacheTimeout;
        public int LlRequestAgentDataCacheTimeoutMs
        {
            get
            {
                return 1000 * m_llRequestAgentDataCacheTimeout;
            }
            set
            {
                m_llRequestAgentDataCacheTimeout = value / 1000;
            }
       }

        protected IScriptEngine m_ScriptEngine;
        protected SceneObjectPart m_host;

        protected UUID RegionScopeID = UUID.Zero;
        protected string m_regionName = String.Empty;
        /// <summary>
        /// The item that hosts this script
        /// </summary>
        protected TaskInventoryItem m_item;

        protected bool throwErrorOnNotImplemented = false;
        protected float m_ScriptDelayFactor = 1.0f;
        protected float m_Script10mDistance = 10.0f;
        protected float m_Script10mDistanceSquare = 100.0f;
        protected float m_MinTimerInterval = 0.5f;
        protected float m_recoilScaleFactor = 0.0f;
        protected bool m_AllowGodFunctions;

        protected string m_GetWallclockTimeZone = String.Empty;     // Defaults to UTC
        protected double m_timer = Util.GetTimeStampMS();
        protected bool m_waitingForScriptAnswer = false;
        protected bool m_automaticLinkPermission = false;
        protected int m_notecardLineReadCharsMax = 255;
        protected int m_scriptConsoleChannel = 0;
        protected bool m_scriptConsoleChannelEnabled = false;
        protected bool m_debuggerSafe = false;

        protected AsyncCommandManager m_AsyncCommands = null;
        protected IUrlModule m_UrlModule = null;
        protected IMaterialsModule m_materialsModule = null;
        protected IEnvironmentModule m_envModule = null;
        protected IEmailModule m_emailModule = null;
        protected IUserAccountService m_userAccountService = null;
        protected IMessageTransferModule m_TransferModule = null;

        protected ExpiringCacheOS<UUID, PresenceInfo> m_PresenceInfoCache = new ExpiringCacheOS<UUID, PresenceInfo>(10000);
        protected int EMAIL_PAUSE_TIME = 20;  // documented delay value for smtp.
        protected int m_sleepMsOnSetTexture = 200;
        protected int m_sleepMsOnSetLinkTexture = 200;
        protected int m_sleepMsOnScaleTexture = 200;
        protected int m_sleepMsOnOffsetTexture = 200;
        protected int m_sleepMsOnRotateTexture = 200;
        protected int m_sleepMsOnSetPos = 200;
        protected int m_sleepMsOnSetRot = 200;
        protected int m_sleepMsOnSetLocalRot = 200;
        protected int m_sleepMsOnPreloadSound = 1000;
        protected int m_sleepMsOnMakeExplosion = 100;
        protected int m_sleepMsOnMakeFountain = 100;
        protected int m_sleepMsOnMakeSmoke = 100;
        protected int m_sleepMsOnMakeFire = 100;
        protected int m_sleepMsOnRezAtRoot = 100;
        protected int m_sleepMsOnInstantMessage = 2000;
        protected int m_sleepMsOnEmail = 30000;
        protected int m_sleepMsOnCreateLink = 1000;
        protected int m_sleepMsOnGiveInventory = 3000;
        protected int m_sleepMsOnRequestAgentData = 100;
        protected int m_sleepMsOnRequestInventoryData = 1000;
        protected int m_sleepMsOnSetDamage = 5000;
        protected int m_sleepMsOnTextBox = 1000;
        protected int m_sleepMsOnAdjustSoundVolume = 100;
        protected int m_sleepMsOnEjectFromLand = 1000;
        protected int m_sleepMsOnAddToLandPassList = 100;
        protected int m_sleepMsOnDialog = 1000;
        protected int m_sleepMsOnRemoteLoadScript = 3000;
        protected int m_sleepMsOnRemoteLoadScriptPin = 3000;
        protected int m_sleepMsOnOpenRemoteDataChannel = 1000;
        protected int m_sleepMsOnSendRemoteData = 3000;
        protected int m_sleepMsOnRemoteDataReply = 3000;
        protected int m_sleepMsOnCloseRemoteDataChannel = 1000;
        protected int m_sleepMsOnSetPrimitiveParams = 200;
        protected int m_sleepMsOnSetLinkPrimitiveParams = 200;
        protected int m_sleepMsOnXorBase64Strings = 300;
        protected int m_sleepMsOnSetParcelMusicURL = 2000;
        protected int m_sleepMsOnGetPrimMediaParams = 1000;
        protected int m_sleepMsOnGetLinkMedia = 1000;
        protected int m_sleepMsOnSetPrimMediaParams = 1000;
        protected int m_sleepMsOnSetLinkMedia = 1000;
        protected int m_sleepMsOnClearPrimMedia = 1000;
        protected int m_sleepMsOnClearLinkMedia = 1000;
        protected int m_sleepMsOnRequestSimulatorData = 1000;
        protected int m_sleepMsOnLoadURL = 1000;
        protected int m_sleepMsOnParcelMediaCommandList = 2000;
        protected int m_sleepMsOnParcelMediaQuery = 2000;
        protected int m_sleepMsOnModPow = 1000;
        protected int m_sleepMsOnSetPrimURL = 2000;
        protected int m_sleepMsOnRefreshPrimURL = 20000;
        protected int m_sleepMsOnMapDestination = 1000;
        protected int m_sleepMsOnAddToLandBanList = 100;
        protected int m_sleepMsOnRemoveFromLandPassList = 100;
        protected int m_sleepMsOnRemoveFromLandBanList = 100;
        protected int m_sleepMsOnResetLandBanList = 100;
        protected int m_sleepMsOnResetLandPassList = 100;
        protected int m_sleepMsOnGetParcelPrimOwners = 2000;
        protected int m_sleepMsOnGetNumberOfNotecardLines = 100;
        protected int m_sleepMsOnGetNotecardLine = 100;
        protected string m_internalObjectHost = "lsl.opensim.local";
        protected bool m_restrictEmail = false;
        protected ISoundModule m_SoundModule = null;

        protected float m_avatarHeightCorrection = 0.2f;
        protected bool m_useSimpleBoxesInGetBoundingBox = false;
        protected bool m_addStatsInGetBoundingBox = false;

        //LSL Avatar Bounding Box (lABB), lower (1) and upper (2),
        //standing (Std), Groundsitting (Grs), Sitting (Sit),
        //along X, Y and Z axes, constants (0) and coefficients (1)
        protected float m_lABB1StdX0 = -0.275f;
        protected float m_lABB2StdX0 = 0.275f;
        protected float m_lABB1StdY0 = -0.35f;
        protected float m_lABB2StdY0 = 0.35f;
        protected float m_lABB1StdZ0 = -0.1f;
        protected float m_lABB1StdZ1 = -0.5f;
        protected float m_lABB2StdZ0 = 0.1f;
        protected float m_lABB2StdZ1 = 0.5f;
        protected float m_lABB1GrsX0 = -0.3875f;
        protected float m_lABB2GrsX0 = 0.3875f;
        protected float m_lABB1GrsY0 = -0.5f;
        protected float m_lABB2GrsY0 = 0.5f;
        protected float m_lABB1GrsZ0 = -0.05f;
        protected float m_lABB1GrsZ1 = -0.375f;
        protected float m_lABB2GrsZ0 = 0.5f;
        protected float m_lABB2GrsZ1 = 0.0f;
        protected float m_lABB1SitX0 = -0.5875f;
        protected float m_lABB2SitX0 = 0.1875f;
        protected float m_lABB1SitY0 = -0.35f;
        protected float m_lABB2SitY0 = 0.35f;
        protected float m_lABB1SitZ0 = -0.35f;
        protected float m_lABB1SitZ1 = -0.375f;
        protected float m_lABB2SitZ0 = -0.25f;
        protected float m_lABB2SitZ1 = 0.25f;

        protected float m_primSafetyCoeffX = 2.414214f;
        protected float m_primSafetyCoeffY = 2.414214f;
        protected float m_primSafetyCoeffZ = 1.618034f;

        protected float m_floatToleranceInCastRay = 0.00001f;
        protected float m_floatTolerance2InCastRay = 0.001f;
        protected DetailLevel m_primLodInCastRay = DetailLevel.Medium;
        protected DetailLevel m_sculptLodInCastRay = DetailLevel.Medium;
        protected DetailLevel m_meshLodInCastRay = DetailLevel.Highest;
        protected DetailLevel m_avatarLodInCastRay = DetailLevel.Medium;
        protected int m_maxHitsInCastRay = 16;
        protected int m_maxHitsPerPrimInCastRay = 16;
        protected int m_maxHitsPerObjectInCastRay = 16;
        protected bool m_detectExitsInCastRay = false;
        protected bool m_doAttachmentsInCastRay = false;
        protected int m_msThrottleInCastRay = 200;
        protected int m_msPerRegionInCastRay = 40;
        protected int m_msPerAvatarInCastRay = 10;
        protected int m_msMinInCastRay = 2;
        protected int m_msMaxInCastRay = 40;
        protected static List<Api.LSL_Api.CastRayCall> m_castRayCalls = new List<Api.LSL_Api.CastRayCall>();
        protected bool m_useMeshCacheInCastRay = true;
        protected static Dictionary<ulong, FacetedMesh> m_cachedMeshes = new Dictionary<ulong, FacetedMesh>();

//        protected Timer m_ShoutSayTimer;
        protected int m_SayShoutCount = 0;
        DateTime m_lastSayShoutCheck;

        private int m_whisperdistance = 10;
        private int m_saydistance = 20;
        private int m_shoutdistance = 100;

        bool m_disable_underground_movement = true;

        private string m_lsl_shard = "OpenSim";
        private string m_lsl_user_agent = string.Empty;

        private static readonly Dictionary<string, string> MovementAnimationsForLSL = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            {"CROUCH", "Crouching"},
            {"CROUCHWALK", "CrouchWalking"},
            {"FALLDOWN", "Falling Down"},
            {"FLY", "Flying"},
            {"FLYSLOW", "FlyingSlow"},
            {"HOVER", "Hovering"},
            {"HOVER_UP", "Hovering Up"},
            {"HOVER_DOWN", "Hovering Down"},
            {"JUMP", "Jumping"},
            {"LAND", "Landing"},
            {"PREJUMP", "PreJumping"},
            {"RUN", "Running"},
            {"SIT","Sitting"},
            {"SITGROUND","Sitting on Ground"},
            {"STAND", "Standing"},
            {"STANDUP", "Standing Up"},
            {"STRIDE","Striding"},
            {"SOFT_LAND", "Soft Landing"},
            {"TURNLEFT", "Turning Left"},
            {"TURNRIGHT", "Turning Right"},
            {"WALK", "Walking"}
        };

        //llHTTPRequest custom headers use control
        // true means fatal error,
        // false means ignore,
        // missing means allowed
        private static readonly Dictionary<string,bool> HttpForbiddenHeaders = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase)
        {
            {"Accept", true},
            {"Accept-Charset", true},
            {"Accept-CH", false},
            {"Accept-CH-Lifetime", false},
            {"Access-Control-Request-Headers", false},
            {"Access-Control-Request-Method", false},
            {"Accept-Encoding", false},
            //{"Accept-Language", false},
            {"Accept-Patch", false}, // it is server side
            {"Accept-Post", false}, // it is server side
            {"Accept-Ranges", false}, // it is server side
            //{"Age", false},
            //{"Allow", false},
            //{"Authorization", false},
            {"Cache-Control", false},
            {"Connection", false},
            {"Content-Length", false},
            //{"Content-Encoding", false},
            //{"Content-Location", false},
            //{"Content-MD5", false},
            //{"Content-Range", false},
            {"Content-Type", true},
            {"Cookie", false},
            {"Cookie2", false},
            {"Date", false},
            {"Device-Memory", false},
            {"DTN", false},
            {"Early-Data", false},
            //{"ETag", false},
            {"Expect", false},
            //{"Expires", false},
            {"Feature-Policy", false},
            {"From", true},
            {"Host", true},
            {"Keep-Alive", false},
            {"If-Match", false},
            {"If-Modified-Since", false},
            {"If-None-Match", false},
            //{"If-Range", false},
            {"If-Unmodified-Since", false},
            //{"Last-Modified", false},
            //{"Location", false},
            {"Max-Forwards", false},
            {"Origin", false},
            {"Pragma", false},
            //{"Proxy-Authenticate", false},
            //{"Proxy-Authorization", false},
            //{"Range", false},
            {"Referer", true},
            //{"Retry-After", false},
            {"Server", false},
            {"Set-Cookie", false},
            {"Set-Cookie2", false},
            {"TE", true},
            {"Trailer", true},
            {"Transfer-Encoding", false},
            {"Upgrade", true},
            {"User-Agent", true},
            {"Vary", false},
            {"Via", true},
            {"Viewport-Width", false},
            {"Warning", false},
            {"Width", false},
            //{"WWW-Authenticate", false},

            {"X-Forwarded-For", false},
            {"X-Forwarded-Host", false},
            {"X-Forwarded-Proto", false},

            {"x-secondlife-shard", false},
            {"x-secondlife-object-name", false},
            {"x-secondlife-object-key", false},
            {"x-secondlife-region", false},
            {"x-secondlife-local-position", false},
            {"x-secondlife-local-velocity", false},
            {"x-secondlife-local-rotation", false},
            {"x-secondlife-owner-name", false},
            {"x-secondlife-owner-key", false},
        };

        private static readonly HashSet<string> HttpForbiddenInHeaders = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            "x-secondlife-shard", "x-secondlife-object-name",  "x-secondlife-object-key",
            "x-secondlife-region", "x-secondlife-local-position", "x-secondlife-local-velocity",
            "x-secondlife-local-rotation",  "x-secondlife-owner-name", "x-secondlife-owner-key",
            "connection", "content-length", "from", "host", "proxy-authorization",
            "referer", "trailer", "transfer-encoding", "via", "authorization"
        };

        public void Initialize(IScriptEngine scriptEngine, SceneObjectPart host, TaskInventoryItem item)
        {
            m_lastSayShoutCheck = DateTime.UtcNow;

            m_ScriptEngine = scriptEngine;
            m_host = host;
            m_item = item;
            m_debuggerSafe = m_ScriptEngine.Config.GetBoolean("DebuggerSafe", false);

            LoadConfig();

            m_TransferModule = m_ScriptEngine.World.RequestModuleInterface<IMessageTransferModule>();
            m_UrlModule = m_ScriptEngine.World.RequestModuleInterface<IUrlModule>();
            m_SoundModule = m_ScriptEngine.World.RequestModuleInterface<ISoundModule>();
            m_materialsModule = m_ScriptEngine.World.RequestModuleInterface<IMaterialsModule>();

            m_emailModule = m_ScriptEngine.World.RequestModuleInterface<IEmailModule>();
            m_envModule = m_ScriptEngine.World.RequestModuleInterface< IEnvironmentModule>();

            m_AsyncCommands = new AsyncCommandManager(m_ScriptEngine);
            m_userAccountService = World.UserAccountService;
            if(World.RegionInfo != null)
            {
                RegionScopeID = World.RegionInfo.ScopeID;
                m_regionName = World.RegionInfo.RegionName;
            }
        }

        /// <summary>
        /// Load configuration items that affect script, object and run-time behavior. */
        /// </summary>
        private void LoadConfig()
        {
            LlRequestAgentDataCacheTimeoutMs = 20000;

            IConfig seConfig = m_ScriptEngine.Config;

            if (seConfig != null)
            {
                float scriptDistanceFactor = seConfig.GetFloat("ScriptDistanceLimitFactor", 1.0f);
                m_Script10mDistance = 10.0f * scriptDistanceFactor;
                m_Script10mDistanceSquare = m_Script10mDistance * m_Script10mDistance;

                m_ScriptDelayFactor = seConfig.GetFloat("ScriptDelayFactor", m_ScriptDelayFactor);
                m_MinTimerInterval         = seConfig.GetFloat("MinTimerInterval", m_MinTimerInterval);
                m_automaticLinkPermission  = seConfig.GetBoolean("AutomaticLinkPermission", m_automaticLinkPermission);
                m_notecardLineReadCharsMax = seConfig.GetInt("NotecardLineReadCharsMax", m_notecardLineReadCharsMax);

                m_GetWallclockTimeZone = seConfig.GetString("GetWallclockTimeZone", m_GetWallclockTimeZone);

                // Rezzing an object with a velocity can create recoil. This feature seems to have been
                //    removed from recent versions of SL. The code computes recoil (vel*mass) and scales
                //    it by this factor. May be zero to turn off recoil all together.
                m_recoilScaleFactor = seConfig.GetFloat("RecoilScaleFactor", m_recoilScaleFactor);
                m_AllowGodFunctions = seConfig.GetBoolean("AllowGodFunctions", false);

                m_disable_underground_movement = seConfig.GetBoolean("DisableUndergroundMovement", true);
            }

            if (m_notecardLineReadCharsMax > 65535)
                m_notecardLineReadCharsMax = 65535;

            // load limits for particular subsystems.
            IConfigSource seConfigSource = m_ScriptEngine.ConfigSource;

            if (seConfigSource != null)
            {
                IConfig netConfig = seConfigSource.Configs["Network"];
                if (netConfig != null)
                {
                    m_lsl_shard = netConfig.GetString("shard", m_lsl_shard);
                    m_lsl_user_agent = netConfig.GetString("user_agent", m_lsl_user_agent);
                }

                IConfig lslConfig = seConfigSource.Configs["LL-Functions"];
                if (lslConfig != null)
                {
                    m_restrictEmail = lslConfig.GetBoolean("RestrictEmail", m_restrictEmail);
                    m_avatarHeightCorrection = lslConfig.GetFloat("AvatarHeightCorrection", m_avatarHeightCorrection);
                    m_useSimpleBoxesInGetBoundingBox = lslConfig.GetBoolean("UseSimpleBoxesInGetBoundingBox", m_useSimpleBoxesInGetBoundingBox);
                    m_addStatsInGetBoundingBox = lslConfig.GetBoolean("AddStatsInGetBoundingBox", m_addStatsInGetBoundingBox);
                    m_lABB1StdX0 = lslConfig.GetFloat("LowerAvatarBoundingBoxStandingXconst", m_lABB1StdX0);
                    m_lABB2StdX0 = lslConfig.GetFloat("UpperAvatarBoundingBoxStandingXconst", m_lABB2StdX0);
                    m_lABB1StdY0 = lslConfig.GetFloat("LowerAvatarBoundingBoxStandingYconst", m_lABB1StdY0);
                    m_lABB2StdY0 = lslConfig.GetFloat("UpperAvatarBoundingBoxStandingYconst", m_lABB2StdY0);
                    m_lABB1StdZ0 = lslConfig.GetFloat("LowerAvatarBoundingBoxStandingZconst", m_lABB1StdZ0);
                    m_lABB1StdZ1 = lslConfig.GetFloat("LowerAvatarBoundingBoxStandingZcoeff", m_lABB1StdZ1);
                    m_lABB2StdZ0 = lslConfig.GetFloat("UpperAvatarBoundingBoxStandingZconst", m_lABB2StdZ0);
                    m_lABB2StdZ1 = lslConfig.GetFloat("UpperAvatarBoundingBoxStandingZcoeff", m_lABB2StdZ1);
                    m_lABB1GrsX0 = lslConfig.GetFloat("LowerAvatarBoundingBoxGroundsittingXconst", m_lABB1GrsX0);
                    m_lABB2GrsX0 = lslConfig.GetFloat("UpperAvatarBoundingBoxGroundsittingXconst", m_lABB2GrsX0);
                    m_lABB1GrsY0 = lslConfig.GetFloat("LowerAvatarBoundingBoxGroundsittingYconst", m_lABB1GrsY0);
                    m_lABB2GrsY0 = lslConfig.GetFloat("UpperAvatarBoundingBoxGroundsittingYconst", m_lABB2GrsY0);
                    m_lABB1GrsZ0 = lslConfig.GetFloat("LowerAvatarBoundingBoxGroundsittingZconst", m_lABB1GrsZ0);
                    m_lABB1GrsZ1 = lslConfig.GetFloat("LowerAvatarBoundingBoxGroundsittingZcoeff", m_lABB1GrsZ1);
                    m_lABB2GrsZ0 = lslConfig.GetFloat("UpperAvatarBoundingBoxGroundsittingZconst", m_lABB2GrsZ0);
                    m_lABB2GrsZ1 = lslConfig.GetFloat("UpperAvatarBoundingBoxGroundsittingZcoeff", m_lABB2GrsZ1);
                    m_lABB1SitX0 = lslConfig.GetFloat("LowerAvatarBoundingBoxSittingXconst", m_lABB1SitX0);
                    m_lABB2SitX0 = lslConfig.GetFloat("UpperAvatarBoundingBoxSittingXconst", m_lABB2SitX0);
                    m_lABB1SitY0 = lslConfig.GetFloat("LowerAvatarBoundingBoxSittingYconst", m_lABB1SitY0);
                    m_lABB2SitY0 = lslConfig.GetFloat("UpperAvatarBoundingBoxSittingYconst", m_lABB2SitY0);
                    m_lABB1SitZ0 = lslConfig.GetFloat("LowerAvatarBoundingBoxSittingZconst", m_lABB1SitZ0);
                    m_lABB1SitZ1 = lslConfig.GetFloat("LowerAvatarBoundingBoxSittingZcoeff", m_lABB1SitZ1);
                    m_lABB2SitZ0 = lslConfig.GetFloat("UpperAvatarBoundingBoxSittingZconst", m_lABB2SitZ0);
                    m_lABB2SitZ1 = lslConfig.GetFloat("UpperAvatarBoundingBoxSittingZcoeff", m_lABB2SitZ1);
                    m_primSafetyCoeffX = lslConfig.GetFloat("PrimBoundingBoxSafetyCoefficientX", m_primSafetyCoeffX);
                    m_primSafetyCoeffY = lslConfig.GetFloat("PrimBoundingBoxSafetyCoefficientY", m_primSafetyCoeffY);
                    m_primSafetyCoeffZ = lslConfig.GetFloat("PrimBoundingBoxSafetyCoefficientZ", m_primSafetyCoeffZ);
                    m_floatToleranceInCastRay = lslConfig.GetFloat("FloatToleranceInLlCastRay", m_floatToleranceInCastRay);
                    m_floatTolerance2InCastRay = lslConfig.GetFloat("FloatTolerance2InLlCastRay", m_floatTolerance2InCastRay);
                    m_primLodInCastRay = (DetailLevel)lslConfig.GetInt("PrimDetailLevelInLlCastRay", (int)m_primLodInCastRay);
                    m_sculptLodInCastRay = (DetailLevel)lslConfig.GetInt("SculptDetailLevelInLlCastRay", (int)m_sculptLodInCastRay);
                    m_meshLodInCastRay = (DetailLevel)lslConfig.GetInt("MeshDetailLevelInLlCastRay", (int)m_meshLodInCastRay);
                    m_avatarLodInCastRay = (DetailLevel)lslConfig.GetInt("AvatarDetailLevelInLlCastRay", (int)m_avatarLodInCastRay);
                    m_maxHitsInCastRay = lslConfig.GetInt("MaxHitsInLlCastRay", m_maxHitsInCastRay);
                    m_maxHitsPerPrimInCastRay = lslConfig.GetInt("MaxHitsPerPrimInLlCastRay", m_maxHitsPerPrimInCastRay);
                    m_maxHitsPerObjectInCastRay = lslConfig.GetInt("MaxHitsPerObjectInLlCastRay", m_maxHitsPerObjectInCastRay);
                    m_detectExitsInCastRay = lslConfig.GetBoolean("DetectExitHitsInLlCastRay", m_detectExitsInCastRay);
                    m_doAttachmentsInCastRay = lslConfig.GetBoolean("DoAttachmentsInLlCastRay", m_doAttachmentsInCastRay);
                    m_msThrottleInCastRay = lslConfig.GetInt("ThrottleTimeInMsInLlCastRay", m_msThrottleInCastRay);
                    m_msPerRegionInCastRay = lslConfig.GetInt("AvailableTimeInMsPerRegionInLlCastRay", m_msPerRegionInCastRay);
                    m_msPerAvatarInCastRay = lslConfig.GetInt("AvailableTimeInMsPerAvatarInLlCastRay", m_msPerAvatarInCastRay);
                    m_msMinInCastRay = lslConfig.GetInt("RequiredAvailableTimeInMsInLlCastRay", m_msMinInCastRay);
                    m_msMaxInCastRay = lslConfig.GetInt("MaximumAvailableTimeInMsInLlCastRay", m_msMaxInCastRay);
                    m_useMeshCacheInCastRay = lslConfig.GetBoolean("UseMeshCacheInLlCastRay", m_useMeshCacheInCastRay);
                }

                IConfig smtpConfig = seConfigSource.Configs["SMTP"];
                if (smtpConfig != null)
                {
                    // there's an smtp config, so load in the snooze time.
                    EMAIL_PAUSE_TIME = smtpConfig.GetInt("email_pause_time", EMAIL_PAUSE_TIME);

                    m_internalObjectHost = smtpConfig.GetString("internal_object_host", m_internalObjectHost);
                }

                IConfig chatConfig = seConfigSource.Configs["SMTP"];
                if(chatConfig != null)
                {
                    m_whisperdistance = chatConfig.GetInt("whisper_distance", m_whisperdistance);
                    m_saydistance = chatConfig.GetInt("say_distance", m_saydistance);
                    m_shoutdistance = chatConfig.GetInt("shout_distance", m_shoutdistance);
                }
            }
            m_sleepMsOnEmail = EMAIL_PAUSE_TIME * 1000;
        }

        public override Object InitializeLifetimeService()
        {
            ILease lease = (ILease)base.InitializeLifetimeService();

            if (lease.CurrentState == LeaseState.Initial)
            {
                lease.InitialLeaseTime = TimeSpan.FromMinutes(0);
//                lease.RenewOnCallTime = TimeSpan.FromSeconds(10.0);
//                lease.SponsorshipTimeout = TimeSpan.FromMinutes(1.0);
            }
            return lease;
        }

        protected SceneObjectPart MonitoringObject()
        {
            UUID m = m_host.ParentGroup.MonitoringObject;
            if (m.IsZero())
                return null;

            SceneObjectPart p = m_ScriptEngine.World.GetSceneObjectPart(m);
            if (p == null)
                m_host.ParentGroup.MonitoringObject = UUID.Zero;

            return p;
        }

        protected virtual void ScriptSleep(int delay)
        {
            delay = (int)(delay * m_ScriptDelayFactor);
            if (delay < 10)
                return;

            Sleep(delay);
        }

        protected virtual void Sleep(int delay)
        {
            if (m_item == null) // Some unit tests don't set this
                Thread.Sleep(delay);
            else
                m_ScriptEngine.SleepScript(m_item.ItemID, delay);
        }

        /// <summary>
        /// Check for co-operative termination.
        /// </summary>
        /// <param name='delay'>If called with 0, then just the check is performed with no wait.</param>

        public Scene World
        {
            get { return m_ScriptEngine.World; }
        }

        [DebuggerNonUserCode]
        public void state(string newState)
        {
            m_ScriptEngine.SetState(m_item.ItemID, newState);
        }


        public List<ScenePresence> GetLinkAvatars(int linkType)
        {
            if (m_host == null)
                return new List<ScenePresence>();

            return GetLinkAvatars(linkType, m_host.ParentGroup);

        }

        public List<ScenePresence> GetLinkAvatars(int linkType, SceneObjectGroup sog)
        {
            List<ScenePresence> ret = new List<ScenePresence>();
            if (sog == null || sog.IsDeleted)
                return ret;

            List<ScenePresence> avs = sog.GetSittingAvatars();
            switch (linkType)
            {
                case ScriptBaseClass.LINK_SET:
                    return avs;

                case ScriptBaseClass.LINK_ROOT:
                    return ret;

                case ScriptBaseClass.LINK_ALL_OTHERS:
                    return avs;

                case ScriptBaseClass.LINK_ALL_CHILDREN:
                    return avs;

                case ScriptBaseClass.LINK_THIS:
                    return ret;

                default:
                    if (linkType < 0)
                        return ret;

                    int partCount = sog.GetPartCount();

                    linkType -= partCount;
                    if (linkType <= 0)
                    {
                        return ret;
                    }
                    else
                    {
                        if (linkType > avs.Count)
                        {
                            return ret;
                        }
                        else
                        {
                            ret.Add(avs[linkType-1]);
                            return ret;
                        }
                    }
            }
        }

        /// <summary>
        /// Get a given link entity from a linkset (linked objects and any sitting avatars).
        /// </summary>
        /// <remarks>
        /// If there are any ScenePresence's in the linkset (i.e. because they are sat upon one of the prims), then
        /// these are counted as extra entities that correspond to linknums beyond the number of prims in the linkset.
        /// The ScenePresences receive linknums in the order in which they sat.
        /// </remarks>
        /// <returns>
        /// The link entity.  null if not found.
        /// </returns>
        /// <param name='part'></param>
        /// <param name='linknum'>
        /// Can be either a non-negative integer or ScriptBaseClass.LINK_THIS (-4).
        /// If ScriptBaseClass.LINK_THIS then the entity containing the script is returned.
        /// If the linkset has one entity and a linknum of zero is given, then the single entity is returned.  If any
        /// positive integer is given in this case then null is returned.
        /// If the linkset has more than one entity and a linknum greater than zero but equal to or less than the number
        /// of entities, then the entity which corresponds to that linknum is returned.
        /// Otherwise, if a positive linknum is given which is greater than the number of entities in the linkset, then
        /// null is returned.
        /// </param>
        public ISceneEntity GetLinkEntity(SceneObjectPart part, int linknum)
        {
            if (linknum < 0)
            {
                if (linknum == ScriptBaseClass.LINK_THIS)
                    return part;
                else
                    return null;
            }

            int actualPrimCount = part.ParentGroup.PrimCount;
            List<ScenePresence> sittingAvatars = part.ParentGroup.GetSittingAvatars();
            int adjustedPrimCount = actualPrimCount + sittingAvatars.Count;

            // Special case for a single prim.  In this case the linknum is zero.  However, this will not match a single
            // prim that has any avatars sat upon it (in which case the root prim is link 1).
            if (linknum == 0)
            {
                if (actualPrimCount == 1 && sittingAvatars.Count == 0)
                    return part;

                return null;
            }
            // Special case to handle a single prim with sitting avatars.  GetLinkPart() would only match zero but
            // here we must match 1 (ScriptBaseClass.LINK_ROOT).
            else if (linknum == ScriptBaseClass.LINK_ROOT && actualPrimCount == 1)
            {
                if (sittingAvatars.Count > 0)
                    return part.ParentGroup.RootPart;
                else
                    return null;
            }
            else if (linknum <= adjustedPrimCount)
            {
                if (linknum <= actualPrimCount)
                {
                    return part.ParentGroup.GetLinkNumPart(linknum);
                }
                else
                {
                    return sittingAvatars[linknum - actualPrimCount - 1];
                }
            }
            else
            {
                return null;
            }
        }

        public List<SceneObjectPart> GetLinkParts(int linkType)
        {
            return GetLinkParts(m_host, linkType);
        }

        public static List<SceneObjectPart> GetLinkParts(SceneObjectPart part, int linkType)
        {
            List<SceneObjectPart> ret = new List<SceneObjectPart>();
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return ret;

            switch (linkType)
            {
                case ScriptBaseClass.LINK_SET:
                    return new List<SceneObjectPart>(part.ParentGroup.Parts);

                case ScriptBaseClass.LINK_ROOT:
                    ret.Add(part.ParentGroup.RootPart);
                    return ret;

                case ScriptBaseClass.LINK_ALL_OTHERS:
                    ret = new List<SceneObjectPart>(part.ParentGroup.Parts);

                    if (ret.Contains(part))
                        ret.Remove(part);

                    return ret;

                case ScriptBaseClass.LINK_ALL_CHILDREN:
                    ret = new List<SceneObjectPart>(part.ParentGroup.Parts);

                    if (ret.Contains(part.ParentGroup.RootPart))
                        ret.Remove(part.ParentGroup.RootPart);
                    return ret;

                case ScriptBaseClass.LINK_THIS:
                    ret.Add(part);
                    return ret;

                default:
                    if (linkType < 0)
                        return ret;

                    SceneObjectPart target = part.ParentGroup.GetLinkNumPart(linkType);
                    if (target == null)
                        return ret;
                    ret.Add(target);
                    return ret;
            }
        }

        public List<ISceneEntity> GetLinkEntities(int linkType)
        {
            return GetLinkEntities(m_host, linkType);
        }

        public List<ISceneEntity> GetLinkEntities(SceneObjectPart part, int linkType)
        {
            List<ISceneEntity> ret;

            switch (linkType)
            {
                case ScriptBaseClass.LINK_SET:
                    return new List<ISceneEntity>(part.ParentGroup.Parts);

                case ScriptBaseClass.LINK_ROOT:
                    return new List<ISceneEntity>() { part.ParentGroup.RootPart };

                case ScriptBaseClass.LINK_ALL_OTHERS:
                    ret = new List<ISceneEntity>(part.ParentGroup.Parts);

                    if (ret.Contains(part))
                        ret.Remove(part);

                    return ret;

                case ScriptBaseClass.LINK_ALL_CHILDREN:
                    ret = new List<ISceneEntity>(part.ParentGroup.Parts);

                    if (ret.Contains(part.ParentGroup.RootPart))
                        ret.Remove(part.ParentGroup.RootPart);

                    List<ScenePresence> avs = part.ParentGroup.GetSittingAvatars();
                    if(avs!= null && avs.Count > 0)
                        ret.AddRange(avs);

                    return ret;

                case ScriptBaseClass.LINK_THIS:
                    return new List<ISceneEntity>() { part };

                default:
                    if (linkType < 0)
                        return new List<ISceneEntity>();

                    ISceneEntity target = GetLinkEntity(part, linkType);
                    if (target == null)
                        return new List<ISceneEntity>();

                    return new List<ISceneEntity>() { target };
            }
        }

        public string resolveName(UUID objecUUID)
        {
            // try avatar username surname
            UserAccount account = m_userAccountService.GetUserAccount(RegionScopeID, objecUUID);
            if (account != null)
            {
                string avatarname = account.Name;
                return avatarname;
            }
            // try an scene object
            SceneObjectPart SOP = World.GetSceneObjectPart(objecUUID);
            if (SOP != null)
            {
                string objectname = SOP.Name;
                return objectname;
            }

            World.Entities.TryGetValue(objecUUID, out EntityBase SensedObject);

            if (SensedObject == null)
            {
                IGroupsModule groups = World.RequestModuleInterface<IGroupsModule>();
                if (groups != null)
                {
                    GroupRecord gr = groups.GetGroupRecord(objecUUID);
                    if (gr != null)
                        return gr.GroupName;
                }
                return String.Empty;
            }

            return SensedObject.Name;
        }


        private bool IsPhysical()
        {
            return ((m_host.GetEffectiveObjectFlags() & (uint)PrimFlags.Physics) == (uint)PrimFlags.Physics);
        }

        protected void SetScale(SceneObjectPart part, LSL_Vector scale)
        {
            // TODO: this needs to trigger a persistance save as well
            if (part == null || part.ParentGroup.IsDeleted)
                return;

            // First we need to check whether or not we need to clamp the size of a physics-enabled prim
            PhysicsActor pa = part.ParentGroup.RootPart.PhysActor;
            if (pa != null && pa.IsPhysical)
            {
                scale.x = Math.Max(World.m_minPhys, Math.Min(World.m_maxPhys, scale.x));
                scale.y = Math.Max(World.m_minPhys, Math.Min(World.m_maxPhys, scale.y));
                scale.z = Math.Max(World.m_minPhys, Math.Min(World.m_maxPhys, scale.z));
            }
            else
            {
                // If not physical, then we clamp the scale to the non-physical min/max
                scale.x = Math.Max(World.m_minNonphys, Math.Min(World.m_maxNonphys, scale.x));
                scale.y = Math.Max(World.m_minNonphys, Math.Min(World.m_maxNonphys, scale.y));
                scale.z = Math.Max(World.m_minNonphys, Math.Min(World.m_maxNonphys, scale.z));
            }

            Vector3 tmp = part.Scale;
            tmp.X = (float)scale.x;
            tmp.Y = (float)scale.y;
            tmp.Z = (float)scale.z;
            part.Scale = tmp;
            part.ParentGroup.HasGroupChanged = true;
            part.SendFullUpdateToAllClients();
        }

        protected void SetColor(SceneObjectPart part, LSL_Vector color, int face)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            m_host.SetFaceColorAlpha(face, color, null);
        }

        public void SetTexGen(SceneObjectPart part, int face,int style)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            Primitive.TextureEntry tex = part.Shape.Textures;
            MappingType textype;
            textype = MappingType.Default;
            if (style == ScriptBaseClass.PRIM_TEXGEN_PLANAR)
                textype = MappingType.Planar;

            int nsides = GetNumberOfSides(part);

            if (face >= 0 && face < nsides)
            {
                tex.CreateFace((uint) face);
                tex.FaceTextures[face].TexMapType = textype;
                part.UpdateTextureEntry(tex);
                return;
            }
            else if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (uint i = 0; i < nsides; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].TexMapType = textype;
                    }
                }
                tex.DefaultTexture.TexMapType = textype;
                part.UpdateTextureEntry(tex);
                return;
            }
        }

        public void SetGlow(SceneObjectPart part, int face, float glow)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            Primitive.TextureEntry tex = part.Shape.Textures;
            int nsides = GetNumberOfSides(part);

            if (face >= 0 && face < nsides)
            {
                tex.CreateFace((uint) face);
                tex.FaceTextures[face].Glow = glow;
                part.UpdateTextureEntry(tex);
                return;
            }
            else if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (uint i = 0; i < nsides; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].Glow = glow;
                    }
                }
                tex.DefaultTexture.Glow = glow;
                part.UpdateTextureEntry(tex);
                return;
            }
        }

        public void SetShiny(SceneObjectPart part, int face, int shiny, Bumpiness bump)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            Shininess sval = new Shininess();

            switch (shiny)
            {
            case 0:
                sval = Shininess.None;
                break;
            case 1:
                sval = Shininess.Low;
                break;
            case 2:
                sval = Shininess.Medium;
                break;
            case 3:
                sval = Shininess.High;
                break;
            default:
                sval = Shininess.None;
                break;
            }

            int nsides = GetNumberOfSides(part);

            Primitive.TextureEntry tex = part.Shape.Textures;
            if (face >= 0 && face < nsides)
            {
                tex.CreateFace((uint) face);
                tex.FaceTextures[face].Shiny = sval;
                tex.FaceTextures[face].Bump = bump;
                part.UpdateTextureEntry(tex);
                return;
            }
            else if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (uint i = 0; i < nsides; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].Shiny = sval;
                        tex.FaceTextures[i].Bump = bump;
                    }
                }
                tex.DefaultTexture.Shiny = sval;
                tex.DefaultTexture.Bump = bump;
                part.UpdateTextureEntry(tex);
                return;
            }
        }

        public void SetFullBright(SceneObjectPart part, int face, bool bright)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

             int nsides = GetNumberOfSides(part);
             Primitive.TextureEntry tex = part.Shape.Textures;
             if (face >= 0 && face < nsides)
             {
                 tex.CreateFace((uint) face);
                 tex.FaceTextures[face].Fullbright = bright;
                 part.UpdateTextureEntry(tex);
                 return;
             }
             else if (face == ScriptBaseClass.ALL_SIDES)
             {
                tex.DefaultTexture.Fullbright = bright;
                for (uint i = 0; i < nsides; i++)
                 {
                    if(tex.FaceTextures[i] != null)
                        tex.FaceTextures[i].Fullbright = bright;
                 }
                 part.UpdateTextureEntry(tex);
                 return;
             }
         }

        protected LSL_Float GetAlpha(SceneObjectPart part, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            int nsides = GetNumberOfSides(part);
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                int i;
                double sum = 0.0;
                for (i = 0 ; i < nsides; i++)
                    sum += (double)tex.GetFace((uint)i).RGBA.A;
                return sum;
            }
            if (face >= 0 && face < nsides)
            {
                return (double)tex.GetFace((uint)face).RGBA.A;
            }
            return 0.0;
        }

        protected void SetAlpha(SceneObjectPart part, double alpha, int face)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            Primitive.TextureEntry tex = part.Shape.Textures;
            int nsides = GetNumberOfSides(part);
            Color4 texcolor;

            if (face >= 0 && face < nsides)
            {
                texcolor = tex.CreateFace((uint)face).RGBA;
                texcolor.A = Utils.Clamp((float)alpha, 0.0f, 1.0f);
                tex.FaceTextures[face].RGBA = texcolor;
                part.UpdateTextureEntry(tex);
                return;
            }
            else if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (int i = 0; i < nsides; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        texcolor = tex.FaceTextures[i].RGBA;
                        texcolor.A = Utils.Clamp((float)alpha, 0.0f, 1.0f);
                        tex.FaceTextures[i].RGBA = texcolor;
                    }
                }

                // In some cases, the default texture can be null, eg when every face
                // has a unique texture
                if (tex.DefaultTexture != null)
                {
                    texcolor = tex.DefaultTexture.RGBA;
                    texcolor.A = Utils.Clamp((float)alpha, 0.0f, 1.0f);
                    tex.DefaultTexture.RGBA = texcolor;
                }

                part.UpdateTextureEntry(tex);
                return;
            }
        }

        /// <summary>
        /// Set flexi parameters of a part.
        ///
        /// FIXME: Much of this code should probably be within the part itself.
        /// </summary>
        /// <param name="part"></param>
        /// <param name="flexi"></param>
        /// <param name="softness"></param>
        /// <param name="gravity"></param>
        /// <param name="friction"></param>
        /// <param name="wind"></param>
        /// <param name="tension"></param>
        /// <param name="Force"></param>
        protected void SetFlexi(SceneObjectPart part, bool flexi, int softness, float gravity, float friction,
            float wind, float tension, LSL_Vector Force)
        {
            if (part == null)
                return;
            SceneObjectGroup sog = part.ParentGroup;

            if(sog == null || sog.IsDeleted || sog.inTransit)
                return;

            PrimitiveBaseShape pbs = part.Shape;
            pbs.FlexiSoftness = softness;
            pbs.FlexiGravity = gravity;
            pbs.FlexiDrag = friction;
            pbs.FlexiWind = wind;
            pbs.FlexiTension = tension;
            pbs.FlexiForceX = (float)Force.x;
            pbs.FlexiForceY = (float)Force.y;
            pbs.FlexiForceZ = (float)Force.z;

            pbs.FlexiEntry = flexi;

            if (!pbs.SculptEntry && (pbs.PathCurve == (byte)Extrusion.Straight || pbs.PathCurve == (byte)Extrusion.Flexible))
            {
                if(flexi)
                {
                    pbs.PathCurve = (byte)Extrusion.Flexible;
                    if(!sog.IsPhantom)
                    {
                        sog.ScriptSetPhantomStatus(true);
                        return;
                    }
                }
                else
                {
                    // Other values not set, they do not seem to be sent to the viewer
                    // Setting PathCurve appears to be what actually toggles the check box and turns Flexi on and off
                    pbs.PathCurve = (byte)Extrusion.Straight;
                }
            }
            part.ParentGroup.HasGroupChanged = true;
            part.ScheduleFullUpdate();
        }

        /// <summary>
        /// Set a light point on a part
        /// </summary>
        /// FIXME: Much of this code should probably be in SceneObjectGroup
        ///
        /// <param name="part"></param>
        /// <param name="light"></param>
        /// <param name="color"></param>
        /// <param name="intensity"></param>
        /// <param name="radius"></param>
        /// <param name="falloff"></param>
        protected void SetPointLight(SceneObjectPart part, bool light, LSL_Vector color, float intensity, float radius, float falloff)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            PrimitiveBaseShape pbs = part.Shape;

            if (light)
            {
                pbs.LightEntry = true;
                pbs.LightColorR = Utils.Clamp((float)color.x, 0.0f, 1.0f);
                pbs.LightColorG = Utils.Clamp((float)color.y, 0.0f, 1.0f);
                pbs.LightColorB = Utils.Clamp((float)color.z, 0.0f, 1.0f);
                pbs.LightIntensity = Utils.Clamp(intensity, 0.0f, 1.0f);
                pbs.LightRadius = Utils.Clamp(radius, 0.1f, 20.0f);
                pbs.LightFalloff = Utils.Clamp(falloff, 0.01f, 2.0f);
            }
            else
            {
                pbs.LightEntry = false;
            }

            part.ParentGroup.HasGroupChanged = true;
            part.ScheduleFullUpdate();
        }

        private void CheckSayShoutTime()
        {
            DateTime now = DateTime.UtcNow;
            if ((now - m_lastSayShoutCheck).Ticks > 10000000) // 1sec
            {
                m_lastSayShoutCheck = now;
                m_SayShoutCount = 0;
            }
            else
                m_SayShoutCount++;
        }

        public void ThrottleSay(int channelID, int timeMs)
        {
            if (channelID == 0)
                CheckSayShoutTime();
            if (m_SayShoutCount >= 11)
                ScriptSleep(timeMs);
        }
        
        
        private double VecDist(LSL_Vector a, LSL_Vector b)
        {
            double dx = a.x - b.x;
            double dy = a.y - b.y;
            double dz = a.z - b.z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private double VecDistSquare(LSL_Vector a, LSL_Vector b)
        {
            double dx = a.x - b.x;
            double dy = a.y - b.y;
            double dz = a.z - b.z;
            return dx * dx + dy * dy + dz * dz;
        }
        
        
        protected LSL_Vector GetColor(SceneObjectPart part, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            Color4 texcolor;
            LSL_Vector rgb = new LSL_Vector();
            int nsides = GetNumberOfSides(part);

            if (face == ScriptBaseClass.ALL_SIDES)
            {
                int i;
                for (i = 0; i < nsides; i++)
                {
                    texcolor = tex.GetFace((uint)i).RGBA;
                    rgb.x += texcolor.R;
                    rgb.y += texcolor.G;
                    rgb.z += texcolor.B;
                }

                float invnsides = 1.0f / (float)nsides;

                rgb.x *= invnsides;
                rgb.y *= invnsides;
                rgb.z *= invnsides;

                return rgb;
            }
            if (face >= 0 && face < nsides)
            {
                texcolor = tex.GetFace((uint)face).RGBA;
                rgb.x = texcolor.R;
                rgb.y = texcolor.G;
                rgb.z = texcolor.B;

                return rgb;
            }
            else
            {
                return new LSL_Vector();
            }
        }

        protected void SetTextureParams(SceneObjectPart part, string texture, double scaleU, double ScaleV,
                    double offsetU, double offsetV, double rotation, int face)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            UUID textureID = new UUID();
            bool dotexture = true;
            if(String.IsNullOrEmpty(texture) || texture == ScriptBaseClass.NULL_KEY)
                dotexture = false;
            else
            {
                textureID = ScriptUtils.GetAssetIdFromItemName(m_host, texture, (int)AssetType.Texture);
                if (textureID.IsZero())
                {
                    if (!UUID.TryParse(texture, out textureID))
                        dotexture = false;
                }
            }

            Primitive.TextureEntry tex = part.Shape.Textures;
            int nsides = GetNumberOfSides(part);

            if (face >= 0 && face < nsides)
            {
                Primitive.TextureEntryFace texface = tex.CreateFace((uint)face);
                if (dotexture)
                    texface.TextureID = textureID;
                texface.RepeatU = (float)scaleU;
                texface.RepeatV = (float)ScaleV;
                texface.OffsetU = (float)offsetU;
                texface.OffsetV = (float)offsetV;
                texface.Rotation = (float)rotation;
                tex.FaceTextures[face] = texface;
                part.UpdateTextureEntry(tex);
                return;
            }
            else if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (uint i = 0; i < nsides; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        if (dotexture)
                            tex.FaceTextures[i].TextureID = textureID;
                        tex.FaceTextures[i].RepeatU = (float)scaleU;
                        tex.FaceTextures[i].RepeatV = (float)ScaleV;
                        tex.FaceTextures[i].OffsetU = (float)offsetU;
                        tex.FaceTextures[i].OffsetV = (float)offsetV;
                        tex.FaceTextures[i].Rotation = (float)rotation;
                    }
                }
                if (dotexture)
                    tex.DefaultTexture.TextureID = textureID;
                tex.DefaultTexture.RepeatU = (float)scaleU;
                tex.DefaultTexture.RepeatV = (float)ScaleV;
                tex.DefaultTexture.OffsetU = (float)offsetU;
                tex.DefaultTexture.OffsetV = (float)offsetV;
                tex.DefaultTexture.Rotation = (float)rotation;
                part.UpdateTextureEntry(tex);
                return;
            }
        }

        protected void SetTexture(SceneObjectPart part, string texture, int face)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            UUID textureID = ScriptUtils.GetAssetIdFromItemName(m_host, texture, (int)AssetType.Texture);
            if (textureID.IsZero())
            {
                if (!UUID.TryParse(texture, out textureID) || textureID.IsZero())
                    return;
            }

            Primitive.TextureEntry tex = part.Shape.Textures;
            int nsides = GetNumberOfSides(part);

            if (face >= 0 && face < nsides)
            {
                Primitive.TextureEntryFace texface = tex.CreateFace((uint)face);
                texface.TextureID = textureID;
                tex.FaceTextures[face] = texface;
                part.UpdateTextureEntry(tex);
                return;
            }
            else if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (uint i = 0; i < nsides; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].TextureID = textureID;
                    }
                }
                tex.DefaultTexture.TextureID = textureID;
                part.UpdateTextureEntry(tex);
                return;
            }
        }
        protected void ScaleTexture(SceneObjectPart part, double u, double v, int face)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            Primitive.TextureEntry tex = part.Shape.Textures;
            int nsides = GetNumberOfSides(part);

            if (face >= 0 && face < nsides)
            {
                Primitive.TextureEntryFace texface = tex.CreateFace((uint)face);
                texface.RepeatU = (float)u;
                texface.RepeatV = (float)v;
                tex.FaceTextures[face] = texface;
                part.UpdateTextureEntry(tex);
                return;
            }
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (int i = 0; i < nsides; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].RepeatU = (float)u;
                        tex.FaceTextures[i].RepeatV = (float)v;
                    }
                }
                tex.DefaultTexture.RepeatU = (float)u;
                tex.DefaultTexture.RepeatV = (float)v;
                part.UpdateTextureEntry(tex);
                return;
            }
        }

        protected void OffsetTexture(SceneObjectPart part, double u, double v, int face)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            Primitive.TextureEntry tex = part.Shape.Textures;
            int nsides = GetNumberOfSides(part);

            if (face >= 0 && face < nsides)
            {
                Primitive.TextureEntryFace texface = tex.CreateFace((uint)face);
                texface.OffsetU = (float)u;
                texface.OffsetV = (float)v;
                tex.FaceTextures[face] = texface;
                part.UpdateTextureEntry(tex);
                return;
            }
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (int i = 0; i < nsides; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].OffsetU = (float)u;
                        tex.FaceTextures[i].OffsetV = (float)v;
                    }
                }
                tex.DefaultTexture.OffsetU = (float)u;
                tex.DefaultTexture.OffsetV = (float)v;
                part.UpdateTextureEntry(tex);
                return;
            }
        }

        protected void RotateTexture(SceneObjectPart part, double rotation, int face)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            Primitive.TextureEntry tex = part.Shape.Textures;
            int nsides = GetNumberOfSides(part);

            if (face >= 0 && face < nsides)
            {
                Primitive.TextureEntryFace texface = tex.CreateFace((uint)face);
                texface.Rotation = (float)rotation;
                tex.FaceTextures[face] = texface;
                part.UpdateTextureEntry(tex);
                return;
            }
            if (face == ScriptBaseClass.ALL_SIDES)
            {
                for (int i = 0; i < nsides; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].Rotation = (float)rotation;
                    }
                }
                tex.DefaultTexture.Rotation = (float)rotation;
                part.UpdateTextureEntry(tex);
                return;
            }
        }

        protected LSL_String GetTexture(SceneObjectPart part, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            int nsides = GetNumberOfSides(part);

            if (face == ScriptBaseClass.ALL_SIDES)
            {
                face = 0;
            }

            if (face >= 0 && face < nsides)
            {
                Primitive.TextureEntryFace texface;
                texface = tex.GetFace((uint)face);
                string texture = texface.TextureID.ToString();

                lock (part.TaskInventory)
                {
                    foreach (KeyValuePair<UUID, TaskInventoryItem> inv in part.TaskInventory)
                    {
                        if (inv.Value.AssetID.Equals(texface.TextureID))
                        {
                            texture = inv.Value.Name.ToString();
                            break;
                        }
                    }
                }

                return texture;
            }
            else
            {
                return ScriptBaseClass.NULL_KEY;
            }
        }

        // Capped movemment if distance > 10m (http://wiki.secondlife.com/wiki/LlSetPos)
        // note linked setpos is capped "differently"
        private LSL_Vector SetPosAdjust(LSL_Vector start, LSL_Vector end)
        {
            if (VecDistSquare(start, end) > m_Script10mDistanceSquare)
                return start + m_Script10mDistance * llVecNorm(end - start);
            else
                return end;
        }

        protected LSL_Vector GetSetPosTarget(SceneObjectPart part, LSL_Vector targetPos, LSL_Vector fromPos, bool adjust)
        {
            if (part == null)
                return targetPos;
            SceneObjectGroup grp = part.ParentGroup;
            if (grp == null || grp.IsDeleted || grp.inTransit)
                return targetPos;

            if (adjust)
                targetPos = SetPosAdjust(fromPos, targetPos);

            if (m_disable_underground_movement && grp.AttachmentPoint == 0)
            {
                if (part.IsRoot)
                {
                    float ground = World.GetGroundHeight((float)targetPos.x, (float)targetPos.y);
                    if ((targetPos.z < ground))
                        targetPos.z = ground;
                }
            }
            return targetPos;
        }

        /// <summary>
        /// set object position, optionally capping the distance.
        /// </summary>
        /// <param name="part"></param>
        /// <param name="targetPos"></param>
        /// <param name="adjust">if TRUE, will cap the distance to 10m.</param>
        protected void SetPos(SceneObjectPart part, LSL_Vector targetPos, bool adjust)
        {
            if (part == null)
                return;

            SceneObjectGroup grp = part.ParentGroup;
            if (grp == null || grp.IsDeleted || grp.inTransit)
                return;

            LSL_Vector currentPos = GetPartLocalPos(part);
            LSL_Vector toPos = GetSetPosTarget(part, targetPos, currentPos, adjust);

            if (part.IsRoot)
            {
                if (!grp.IsAttachment && !World.Permissions.CanObjectEntry(grp, false, (Vector3)toPos))
                    return;
                grp.UpdateGroupPosition((Vector3)toPos);
            }
            else
            {
                part.OffsetPosition = (Vector3)toPos;
//                SceneObjectGroup parent = part.ParentGroup;
//                parent.HasGroupChanged = true;
//                parent.ScheduleGroupForTerseUpdate();
                part.ScheduleTerseUpdate();
            }
        }

        protected LSL_Vector GetPartLocalPos(SceneObjectPart part)
        {
            Vector3 pos;
            if (part.IsRoot)
            {
                if (part.ParentGroup.IsAttachment)
                    pos = part.AttachedPos;
                else
                    pos = part.AbsolutePosition;
            }
            else
            {
                pos = part.OffsetPosition;
            }

            //m_log.DebugFormat("[LSL API]: Returning {0} in GetPartLocalPos()", pos);
            return new LSL_Vector(pos);
        }

        protected void SetRot(SceneObjectPart part, Quaternion rot)
        {
            if (part == null || part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            bool isroot = (part == part.ParentGroup.RootPart);
            bool isphys;

            PhysicsActor pa = part.PhysActor;

            // keep using physactor ideia of isphysical
            // it should be SOP ideia of that
            // not much of a issue with ubOde
            if (pa != null && pa.IsPhysical)
                isphys = true;
            else
                isphys = false;

            // SL doesn't let scripts rotate root of physical linksets
            if (isroot && isphys)
                return;

            part.UpdateRotation(rot);

            // Update rotation does not move the object in the physics engine if it's a non physical linkset
            // so do a nasty update of parts positions if is a root part rotation
            if (isroot && pa != null) // with if above implies non physical  root part
            {
                part.ParentGroup.ResetChildPrimPhysicsPositions();
            }
            else // fix sitting avatars. This is only needed bc of how we link avas to child parts, not root part
            {
                //                List<ScenePresence> sittingavas = part.ParentGroup.GetLinkedAvatars();
                List<ScenePresence> sittingavas = part.ParentGroup.GetSittingAvatars();
                if (sittingavas.Count > 0)
                {
                    foreach (ScenePresence av in sittingavas)
                    {
                        if (isroot || part.LocalId == av.ParentID)
                            av.SendTerseUpdateToAllClients();
                    }
                }
            }
        }


        private LSL_Rotation GetPartRot(SceneObjectPart part)
        {
            Quaternion q;
            if (part.LinkNum == 0 || part.LinkNum == 1) // unlinked or root prim
            {
                if (part.ParentGroup.AttachmentPoint != 0)
                {
                    ScenePresence avatar = World.GetScenePresence(part.ParentGroup.AttachedAvatar);
                    if (avatar != null)
                    {
                        if ((avatar.AgentControlFlags & (uint)AgentManager.ControlFlags.AGENT_CONTROL_MOUSELOOK) != 0)
                            q = avatar.CameraRotation; // Mouselook
                        else
                            q = avatar.GetWorldRotation(); // Currently infrequently updated so may be inaccurate
                    }
                    else
                        q = part.ParentGroup.GroupRotation; // Likely never get here but just in case
                }
                else
                    q = part.ParentGroup.GroupRotation; // just the group rotation

                return new LSL_Rotation(q);
            }

            q = part.GetWorldRotation();
            if (part.ParentGroup.AttachmentPoint != 0)
            {
                ScenePresence avatar = World.GetScenePresence(part.ParentGroup.AttachedAvatar);
                if (avatar != null)
                {
                    if ((avatar.AgentControlFlags & (uint)AgentManager.ControlFlags.AGENT_CONTROL_MOUSELOOK) != 0)
                        q = avatar.CameraRotation * q; // Mouselook
                    else
                        q = avatar.Rotation * q; // Currently infrequently updated so may be inaccurate
                }
            }

            return new LSL_Rotation(q.X, q.Y, q.Z, q.W);
        }

        private LSL_Rotation GetPartLocalRot(SceneObjectPart part)
        {
            Quaternion rot = part.RotationOffset;
            return new LSL_Rotation(rot.X, rot.Y, rot.Z, rot.W);
        }

        public void doObjectRez(string inventory, LSL_Vector pos, LSL_Vector vel, LSL_Rotation rot, int param, bool atRoot)
        {
            if (string.IsNullOrEmpty(inventory) || Double.IsNaN(rot.x) || Double.IsNaN(rot.y) || Double.IsNaN(rot.z) || Double.IsNaN(rot.s))
                return;

            if (VecDistSquare(llGetPos(), pos) > m_Script10mDistanceSquare)
                return;

            TaskInventoryItem item = m_host.Inventory.GetInventoryItem(inventory);

            if (item == null)
            {
               Error("llRez(AtRoot/Object)", "Can't find object '" + inventory + "'");
               return;
            }

            if (item.InvType != (int)InventoryType.Object)
            {
               Error("llRez(AtRoot/Object)", "Can't create requested object; object is missing from database");
               return;
            }

            Util.FireAndForget(x =>
            {
                Quaternion wrot = rot;
                wrot.Normalize();
                List<SceneObjectGroup> new_groups = World.RezObject(m_host, item, pos, wrot, vel, param, atRoot);

                // If either of these are null, then there was an unknown error.
                if (new_groups == null)
                    return;

                bool notAttachment = !m_host.ParentGroup.IsAttachment;

                foreach (SceneObjectGroup group in new_groups)
                {
                    // objects rezzed with this method are die_at_edge by default.
                    group.RootPart.SetDieAtEdge(true);

                    group.ResumeScripts();

                    m_ScriptEngine.PostObjectEvent(m_host.LocalId, new EventParams(
                            "object_rez", new Object[] {
                            new LSL_String(
                            group.RootPart.UUID.ToString()) },
                            new DetectParams[0]));

                    if (notAttachment)
                    {
                        float groupmass = group.GetMass();

                        PhysicsActor pa = group.RootPart.PhysActor;

                        //Recoil.
                        if (pa != null && pa.IsPhysical && !((Vector3)vel).IsZero())
                        {
                            Vector3 recoil = -vel * groupmass * m_recoilScaleFactor;
                            if (!recoil.IsZero())
                            {
                                llApplyImpulse(recoil, 0);
                            }
                        }
                    }
                 }
            }, null, "LSL_Api.doObjectRez");

            //ScriptSleep((int)((groupmass * velmag) / 10));
            ScriptSleep(m_sleepMsOnRezAtRoot);
        }
        
        
        /// <summary>
        /// Attach the object containing this script to the avatar that owns it.
        /// </summary>
        /// <param name='attachmentPoint'>
        /// The attachment point (e.g. <see cref="OpenSim.Region.ScriptEngine.Shared.ScriptBase.ScriptBaseClass.ATTACH_CHEST">ATTACH_CHEST</see>)
        /// </param>
        /// <returns>true if the attach suceeded, false if it did not</returns>
        public bool AttachToAvatar(int attachmentPoint)
        {
            SceneObjectGroup grp = m_host.ParentGroup;
            ScenePresence presence = World.GetScenePresence(m_host.OwnerID);

            IAttachmentsModule attachmentsModule = m_ScriptEngine.World.AttachmentsModule;

            if (attachmentsModule != null)
                return attachmentsModule.AttachObject(presence, grp, (uint)attachmentPoint, false, true, true);
            else
                return false;
        }

        /// <summary>
        /// Detach the object containing this script from the avatar it is attached to.
        /// </summary>
        /// <remarks>
        /// Nothing happens if the object is not attached.
        /// </remarks>
        public void DetachFromAvatar()
        {
            Util.FireAndForget(DetachWrapper, m_host, "LSL_Api.DetachFromAvatar");
        }

        private void DetachWrapper(object o)
        {
            if (World.AttachmentsModule != null)
            {
                SceneObjectPart host = (SceneObjectPart)o;
                ScenePresence presence = World.GetScenePresence(host.OwnerID);
                World.AttachmentsModule.DetachSingleAttachmentToInv(presence, host.ParentGroup);
            }
        }

    }
}