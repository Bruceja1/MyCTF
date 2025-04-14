using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MCGalaxy;

namespace MyCTF;

public class MyCTFTimer
{
    private int seconds;
    private int minutes;
    private int lengthInSeconds;
    public bool timeout;
    public bool secondHasPassed;
    DateTime dateTime = DateTime.UtcNow;
    TimeSpan timeSpan;

    public MyCTFTimer()
    {
        minutes = 0;
        seconds = 0;
    }

    public void Set(int lengthInMinutes)
    {
        lengthInSeconds = lengthInMinutes * 60;
        minutes = lengthInMinutes;
        seconds = 0;
        timeout = false;
    }
    public void DoTimer()
    {
        if (timeout)
        {
            return;
        }
                                     
        if (lengthInSeconds > 0)
        {          
            timeSpan = DateTime.UtcNow - dateTime;
            if (timeSpan.TotalSeconds < 1)
            {
                secondHasPassed = false;
                return;
            }
            secondHasPassed = true;
            lengthInSeconds--;
            if (seconds > 0)
            {
                seconds--;
            }
            else if (seconds == 0)
            {
                minutes--;
                seconds = 59;
            }
            dateTime = DateTime.UtcNow;
            return;
        }
        timeout = true;
    }

    public void Stop()
    {
        timeout = true;
    }

    public string Display()
    {
        string minutes = this.minutes.ToString();
        if (this.minutes < 10)
        {
            minutes = "0" + minutes;
        }
        string seconds = this.seconds.ToString();
        if (this.seconds < 10)
        {
            seconds = "0" + seconds;
        }
        return minutes + ":" + seconds;
    }

    public int GetSecondsLeft()
    {
        return lengthInSeconds;
    }
}
