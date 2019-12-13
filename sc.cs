



//#define NORMAL
//#define NORMAL_TASK
//#define SYNCOBJ_TASK

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Threading.Tasks;
using System.Threading;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
//using ​Mathematics;

//#define TEST

public class sc : MonoBehaviour
{
    ~ sc()
    {
        job.Dispose();
    }
    public static int loopnum = 5000;
    public static int parallels = 4;
    static Task[] tasks;
    static Matrix4x4[] m = new Matrix4x4 [loopnum];
    static Matrix4x4[] t = new Matrix4x4 [loopnum];
    static Matrix4x4[] s = new Matrix4x4 [loopnum];

    static AutoResetEvent[] start = new AutoResetEvent[parallels];
    static AutoResetEvent[] end = new AutoResetEvent[parallels];

    JobTest job;

    // Start is called before the first frame update
    void Start()
    {    
        tasks = new Task[parallels];

        job = new JobTest();
        job.init();

#if SYNCOBJ_TASK
        for(int i=0; i<parallels; i++)
        {
            tasks[i] = MakeTask(i);

            start[i] = new AutoResetEvent(false);
            end[i] = new AutoResetEvent(false);
        }
#endif
                
    }

    // Update is called once per frame\
    void Update()
    {
#if NORMAL      

        for(int i=0; i<loopnum; i++)
        {
            t[i] = Matrix4x4.identity;
            s[i] = Matrix4x4.identity;
            m[i] = t[i]*s[i];
        }
 #elif NORMAL_TASK

        for(int i=0; i<parallels; i++)
        {
            tasks[i] = MakeTask(i);
        }
        for(int i=0; i<parallels; i++)
        {
            tasks[i].Wait();
        }
 #elif SYNCOBJ_TASK
        Set(start);
        Wait(end);
        //Debug.Log("task end" );
 #else

        //Parallel.For(0, loopnum,  i=> {Calc(i) ;} );
        //job.init();
        JobHandle h= job.Schedule(loopnum, 32); 
        h.Complete();
        //job.Dispose();   
 #endif
    }
    static Task MakeTask(int i)
    {
    #if NORMAL_TASK
        return Task.Run(() => { doWork(i); });
    #else
        return Task.Run(() => { doLoopWork(i); });
    #endif    

    }

    static void doWork(int p)
    {
        int number = p;
        //Debug.Log("doWork number " + number);
        int index=0;
    
        for(int i=0; i < (int)(loopnum/parallels); i++)
        {
            int offset = (int)(loopnum/parallels);
            index = offset*number + i;
            t[index] = Matrix4x4.identity;
            s[index] = Matrix4x4.identity;
            m[index] = t[index]*s[index];
        }
        //Debug.Log(/*"doWork index " + index +*/"number"+ number);
    }
    
    static void doLoopWork(int p)
    {
        int number = p;
        while(true) 
        {            
            //Debug.Log("doWork number " + number);

            Wait(start);            
            for(int i=0; i < (int)(loopnum/parallels); i++)
            {
                int offset = (int)(loopnum/parallels);
                int index = offset*number + i;
                t[index] = Matrix4x4.identity;
                s[index] = Matrix4x4.identity;
                m[index] = t[index]*s[index];
            }
            Set(end);
        }
    }    
    static void Set(AutoResetEvent[] syncObj)
    {
        for(int i=0; i<parallels; i++){
            syncObj[i].Set();
        }
    }
    static void Wait(AutoResetEvent [] syncObj)
    {
        for(int i=0; i<parallels; i++)
        {    
            syncObj[i].WaitOne();
        }
    }

    static void Calc(int i )
    {
        t[i] = Matrix4x4.identity;
        s[i] = Matrix4x4.identity;
        m[i] = t[i]*s[i];              
    }    
}


    [BurstCompile] 
    struct JobTest : IJobParallelFor
    {
        NativeArray<Matrix4x4> m;

        NativeArray<Matrix4x4> t;
        NativeArray<Matrix4x4> s;

        
        public void init()
        {
            m = new NativeArray<Matrix4x4>(sc.loopnum, Allocator.Persistent);
            t = new NativeArray<Matrix4x4>(sc.loopnum, Allocator.Persistent);
            s = new NativeArray<Matrix4x4>(sc.loopnum, Allocator.Persistent);
        }
        public void Execute(int i)
        {
            m[i] = Matrix4x4.identity;
            t[i] = Matrix4x4.identity;
            s[i] = m[i]*t[i];

            //Debug.Log("index" + i);
        }

        public void Dispose()
        {
            m.Dispose();
            t.Dispose();
            s.Dispose();
        }
    }