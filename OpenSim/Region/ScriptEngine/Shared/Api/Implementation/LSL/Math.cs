using System;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.ScriptBase;

namespace OpenSim.Region.ScriptEngine.Shared.Api.LSL
{
    public partial class LSL_Api: MarshalByRefObject, ILSL_Api, IScriptApi
    {
        
        //These are the implementations of the various ll-functions used by the LSL scripts.
        public LSL_Float llSin(double f)
        {
            return Math.Sin(f);
        }

        public LSL_Float llCos(double f)
        {
            return Math.Cos(f);
        }

        public LSL_Float llTan(double f)
        {
            return Math.Tan(f);
        }

        public LSL_Float llAtan2(LSL_Float x, LSL_Float y)
        {
            return Math.Atan2(x, y);
        }

        public LSL_Float llSqrt(double f)
        {
            return Math.Sqrt(f);
        }

        public LSL_Float llPow(double fbase, double fexponent)
        {
            return (double)Math.Pow(fbase, fexponent);
        }

        public LSL_Integer llAbs(LSL_Integer i)
        {
            // changed to replicate LSL behaviour whereby minimum int value is returned untouched.
            if (i == Int32.MinValue)
                return i;
            else
                return Math.Abs(i);
        }

        public LSL_Float llFabs(double f)
        {
            return (double)Math.Abs(f);
        }

        public LSL_Float llFrand(double mag)
        {
            lock (Util.RandomClass)
            {
                return Util.RandomClass.NextDouble() * mag;
            }
        }

        public LSL_Integer llFloor(double f)
        {
            return (int)Math.Floor(f);
        }

        public LSL_Integer llCeil(double f)
        {
            return (int)Math.Ceiling(f);
        }

        // Xantor 01/May/2008 fixed midpointrounding (2.5 becomes 3.0 instead of 2.0, default = ToEven)
        public LSL_Integer llRound(double f)
        {
            return (int)Math.Round(f, MidpointRounding.AwayFromZero);
        }

        //This next group are vector operations involving squaring and square root. ckrinke
        public LSL_Float llVecMag(LSL_Vector v)
        {
            return LSL_Vector.Mag(v);
        }

        public LSL_Vector llVecNorm(LSL_Vector v)
        {
            return LSL_Vector.Norm(v);
        }


        public LSL_Float llVecDist(LSL_Vector a, LSL_Vector b)
        {
            return VecDist(a, b);
        }

        //Now we start getting into quaternions which means sin/cos, matrices and vectors. ckrinke

        // Utility function for llRot2Euler

        public LSL_Vector llRot2Euler(LSL_Rotation q1)
        {
            LSL_Vector eul = new LSL_Vector();

            double sqw = q1.s*q1.s;
            double sqx = q1.x*q1.x;
            double sqy = q1.z*q1.z;
            double sqz = q1.y*q1.y;
            double unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
            double test = q1.x*q1.z + q1.y*q1.s;
            if (test > 0.4999*unit) { // singularity at north pole
                eul.z = 2 * Math.Atan2(q1.x,q1.s);
                eul.y = Math.PI/2;
                eul.x = 0;
                return eul;
            }
            if (test < -0.4999*unit) { // singularity at south pole
                eul.z = -2 * Math.Atan2(q1.x,q1.s);
                eul.y = -Math.PI/2;
                eul.x = 0;
                return eul;
            }
            eul.z = Math.Atan2(2*q1.z*q1.s-2*q1.x*q1.y , sqx - sqy - sqz + sqw);
            eul.y = Math.Asin(2*test/unit);
            eul.x = Math.Atan2(2*q1.x*q1.s-2*q1.z*q1.y , -sqx + sqy - sqz + sqw);
            return eul;
        }

        public LSL_Rotation llEuler2Rot(LSL_Vector v)
        {

            double x,y,z,s;
            v.x *= 0.5;
            v.y *= 0.5;
            v.z *= 0.5;
            double c1 = Math.Cos(v.x);
            double c2 = Math.Cos(v.y);
            double c1c2 = c1 * c2;
            double s1 = Math.Sin(v.x);
            double s2 = Math.Sin(v.y);
            double s1s2 = s1 * s2;
            double c1s2 = c1 * s2;
            double s1c2 = s1 * c2;
            double c3 = Math.Cos(v.z);
            double s3 = Math.Sin(v.z);

            x = s1c2 * c3 + c1s2 * s3;
            y = c1s2 * c3 - s1c2 * s3;
            z = s1s2 * c3 + c1c2 * s3;
            s = c1c2 * c3 - s1s2 * s3;

            return new LSL_Rotation(x, y, z, s);
        }

        public LSL_Rotation llAxes2Rot(LSL_Vector fwd, LSL_Vector left, LSL_Vector up)
        {
            double s;
            double tr = fwd.x + left.y + up.z + 1.0;

            if (tr >= 1.0)
            {
                s = 0.5 / Math.Sqrt(tr);
                return new LSL_Rotation(
                        (left.z - up.y) * s,
                        (up.x - fwd.z) * s,
                        (fwd.y - left.x) * s,
                        0.25 / s);
            }
            else
            {
                double max = (left.y > up.z) ? left.y : up.z;

                if (max < fwd.x)
                {
                    s = Math.Sqrt(fwd.x - (left.y + up.z) + 1.0);
                    double x = s * 0.5;
                    s = 0.5 / s;
                    return new LSL_Rotation(
                            x,
                            (fwd.y + left.x) * s,
                            (up.x + fwd.z) * s,
                            (left.z - up.y) * s);
                }
                else if (max == left.y)
                {
                    s = Math.Sqrt(left.y - (up.z + fwd.x) + 1.0);
                    double y = s * 0.5;
                    s = 0.5 / s;
                    return new LSL_Rotation(
                            (fwd.y + left.x) * s,
                            y,
                            (left.z + up.y) * s,
                            (up.x - fwd.z) * s);
                }
                else
                {
                    s = Math.Sqrt(up.z - (fwd.x + left.y) + 1.0);
                    double z = s * 0.5;
                    s = 0.5 / s;
                    return new LSL_Rotation(
                            (up.x + fwd.z) * s,
                            (left.z + up.y) * s,
                            z,
                            (fwd.y - left.x) * s);
                }
            }
        }

        public LSL_Vector llRot2Fwd(LSL_Rotation r)
        {

            double x, y, z, m;

            m = r.x * r.x + r.y * r.y + r.z * r.z + r.s * r.s;
            // m is always greater than zero
            // if m is not equal to 1 then Rotation needs to be normalized
            if (Math.Abs(1.0 - m) > 0.000001) // allow a little slop here for calculation precision
            {
                m = 1.0 / Math.Sqrt(m);
                r.x *= m;
                r.y *= m;
                r.z *= m;
                r.s *= m;
            }

            // Fast Algebric Calculations instead of Vectors & Quaternions Product
            x = r.x * r.x - r.y * r.y - r.z * r.z + r.s * r.s;
            y = 2 * (r.x * r.y + r.z * r.s);
            z = 2 * (r.x * r.z - r.y * r.s);
            return (new LSL_Vector(x, y, z));
        }

        public LSL_Vector llRot2Left(LSL_Rotation r)
        {

            double x, y, z, m;

            m = r.x * r.x + r.y * r.y + r.z * r.z + r.s * r.s;
            // m is always greater than zero
            // if m is not equal to 1 then Rotation needs to be normalized
            if (Math.Abs(1.0 - m) > 0.000001) // allow a little slop here for calculation precision
            {
                m = 1.0 / Math.Sqrt(m);
                r.x *= m;
                r.y *= m;
                r.z *= m;
                r.s *= m;
            }

            // Fast Algebric Calculations instead of Vectors & Quaternions Product
            x = 2 * (r.x * r.y - r.z * r.s);
            y = -r.x * r.x + r.y * r.y - r.z * r.z + r.s * r.s;
            z = 2 * (r.x * r.s + r.y * r.z);
            return (new LSL_Vector(x, y, z));
        }

        public LSL_Vector llRot2Up(LSL_Rotation r)
        {
            double x, y, z, m;

            m = r.x * r.x + r.y * r.y + r.z * r.z + r.s * r.s;
            // m is always greater than zero
            // if m is not equal to 1 then Rotation needs to be normalized
            if (Math.Abs(1.0 - m) > 0.000001) // allow a little slop here for calculation precision
            {
                m = 1.0 / Math.Sqrt(m);
                r.x *= m;
                r.y *= m;
                r.z *= m;
                r.s *= m;
            }

            // Fast Algebric Calculations instead of Vectors & Quaternions Product
            x = 2 * (r.x * r.z + r.y * r.s);
            y = 2 * (-r.x * r.s + r.y * r.z);
            z = -r.x * r.x - r.y * r.y + r.z * r.z + r.s * r.s;
            return (new LSL_Vector(x, y, z));
        }

        public LSL_Rotation llRotBetween(LSL_Vector a, LSL_Vector b)
        {
            //A and B should both be normalized

            // This method mimics the 180 errors found in SL
            // See www.euclideanspace.com... angleBetween

            // Eliminate zero length
            LSL_Float vec_a_mag = LSL_Vector.MagSquare(a);
            LSL_Float vec_b_mag = LSL_Vector.MagSquare(b);
            if (vec_a_mag < 1e-12 ||
                vec_b_mag < 1e-12)
            {
                return new LSL_Rotation(0.0f, 0.0f, 0.0f, 1.0f);
            }

            // Normalize
            a = llVecNorm(a);
            b = llVecNorm(b);

            // Calculate axis and rotation angle
            LSL_Vector axis = a % b;
            LSL_Float cos_theta  = a * b;

            // Check if parallel
            if (cos_theta > 0.99999)
            {
                return new LSL_Rotation(0.0f, 0.0f, 0.0f, 1.0f);
            }

            // Check if anti-parallel
            else if (cos_theta < -0.99999)
            {
                LSL_Vector orthog_axis = new LSL_Vector(1.0, 0.0, 0.0) - (a.x / (a * a) * a);
                if (LSL_Vector.MagSquare(orthog_axis)  < 1e-12)
                    orthog_axis = new LSL_Vector(0.0, 0.0, 1.0);
                return new LSL_Rotation((float)orthog_axis.x, (float)orthog_axis.y, (float)orthog_axis.z, 0.0);
            }
            else // other rotation
            {
                LSL_Float theta = (LSL_Float)Math.Acos(cos_theta) * 0.5f;
                axis = llVecNorm(axis);
                double x, y, z, s, t;
                s = Math.Cos(theta);
                t = Math.Sin(theta);
                x = axis.x * t;
                y = axis.y * t;
                z = axis.z * t;
                return new LSL_Rotation(x,y,z,s);
            }
        }
        
        public LSL_Float llGround(LSL_Vector offset)
        {
            Vector3 pos = m_host.GetWorldPosition() + (Vector3)offset;

            //Get the slope normal.  This gives us the equation of the plane tangent to the slope.
            LSL_Vector vsn = llGroundNormal(offset);

            // Clamp to valid position
            if (pos.X < 0)
                pos.X = 0;
            else if (pos.X >= World.Heightmap.Width)
                pos.X = World.Heightmap.Width - 1;
            if (pos.Y < 0)
                pos.Y = 0;
            else if (pos.Y >= World.Heightmap.Height)
                pos.Y = World.Heightmap.Height - 1;

            //Get the height for the integer coordinates from the Heightmap
            float baseheight = (float)World.Heightmap[(int)pos.X, (int)pos.Y];

            //Calculate the difference between the actual coordinates and the integer coordinates
            float xdiff = pos.X - (float)((int)pos.X);
            float ydiff = pos.Y - (float)((int)pos.Y);

            //Use the equation of the tangent plane to adjust the height to account for slope

            return (((vsn.x * xdiff) + (vsn.y * ydiff)) / (-1 * vsn.z)) + baseheight;
        }

        public LSL_Float llCloud(LSL_Vector offset)
        {
            float cloudCover = 0f;
            ICloudModule module = World.RequestModuleInterface<ICloudModule>();
            if (module != null)
            {
                Vector3 pos = m_host.GetWorldPosition();
                int x = (int)(pos.X + offset.x);
                int y = (int)(pos.Y + offset.y);

                cloudCover = module.CloudCover(x, y, 0);

            }
            return cloudCover;
        }

        public LSL_Vector llWind(LSL_Vector offset)
        {
            LSL_Vector wind = new LSL_Vector(0, 0, 0);
            IWindModule module = World.RequestModuleInterface<IWindModule>();
            if (module != null)
            {
                Vector3 pos = m_host.GetWorldPosition();
                int x = (int)(pos.X + offset.x);
                int y = (int)(pos.Y + offset.y);

                Vector3 windSpeed = module.WindSpeed(x, y, 0);

                wind.x = windSpeed.X;
                wind.y = windSpeed.Y;
            }
            return wind;
        }
        

        public void llApplyImpulse(LSL_Vector force, LSL_Integer local)
        {
            //No energy force yet
            Vector3 v = force;
            if (v.Length() > 20000.0f)
            {
                v.Normalize();
                v = v * 20000.0f;
            }
            m_host.ApplyImpulse(v, local != 0);
        }


        public void llApplyRotationalImpulse(LSL_Vector force, int local)
        {
            m_host.ParentGroup.RootPart.ApplyAngularImpulse(force, local != 0);
        }

        public void llSetTorque(LSL_Vector torque, int local)
        {
            m_host.ParentGroup.RootPart.SetAngularImpulse(torque, local != 0);
        }

        public LSL_Vector llGetTorque()
        {

            return new LSL_Vector(m_host.ParentGroup.GetTorque());
        }

        public void llSetForceAndTorque(LSL_Vector force, LSL_Vector torque, int local)
        {
            llSetForce(force, local);
            llSetTorque(torque, local);
        }


        public LSL_Vector llGetVel()
        {

            Vector3 vel = Vector3.Zero;

            if (m_host.ParentGroup.IsAttachment)
            {
                ScenePresence avatar = m_host.ParentGroup.Scene.GetScenePresence(m_host.ParentGroup.AttachedAvatar);
                if (avatar != null)
                    vel = avatar.GetWorldVelocity();
            }
            else
            {
                vel = m_host.ParentGroup.RootPart.Velocity;
            }

            return new LSL_Vector(vel);
        }

        public LSL_Vector llGetAccel()
        {

            return new LSL_Vector(m_host.Acceleration);
        }

        public LSL_Vector llGetOmega()
        {
            Vector3 avel = m_host.AngularVelocity;
            return new LSL_Vector(avel.X, avel.Y, avel.Z);
        }

        public LSL_Float llGetTimeOfDay()
        {
            return (double)((DateTime.Now.TimeOfDay.TotalMilliseconds / 1000) % (3600 * 4));
        }

        public LSL_Float llGetWallclock()
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.Now;

            if (string.IsNullOrEmpty(m_GetWallclockTimeZone) == false)
            {
                dateTimeOffset = 
                    TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                        DateTimeOffset.UtcNow, m_GetWallclockTimeZone);
            }

            return Math.Truncate(dateTimeOffset.DateTime.TimeOfDay.TotalSeconds);
        }

        public LSL_Float llGetTime()
        {
            double ScriptTime = Util.GetTimeStampMS() - m_timer;
            return (float)Math.Round((ScriptTime / 1000.0), 3);
        }

        public void llResetTime()
        {
            m_timer = Util.GetTimeStampMS();
        }

        public LSL_Float llGetAndResetTime()
        {
            double now = Util.GetTimeStampMS();
            double ScriptTime = now - m_timer;
            m_timer = now;
            return (float)Math.Round((ScriptTime / 1000.0), 3);
        }

        
    }
}