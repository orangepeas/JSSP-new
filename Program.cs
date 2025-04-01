using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

class JobShopSchedule
{
    private List<Job> jobs;
    private List<SubJob> subjobs;
    public List<Schedule> schedules = new List<Schedule>();
    public List<Schedule> currentJobShopSchedule = new List<Schedule>();

    public JobShopSchedule(List<Job> jobs, List<SubJob> subJobs)
    {
        this.jobs = jobs;
        this.subjobs = subjobs;

        //Schedules creator
        foreach (Job job in jobs)
        {
            foreach (SubJob subjob in job.subJobs) {
                var matches = schedules.Where(s => s.name == subjob.subdivision).ToList();
                if (matches.Count == 0)
                {
                    Schedule s = new Schedule(subjob.subdivision, subjob.operationId);
                    schedules.Add(s);
                }
            }
        }
        schedules.OrderBy(s => s.operationId).ToList();
        GenerateSchedule();
    }

    public List<SubJob> GenerateSchedule()
    {
        //randomise which subdivision the order to place the subdivisions in the job in, which job to start with
        //then order by subjob operation id/start time, whatever
        //place them in order of the operation in their machines
        //then do that for each job, taking into account previous jobs

        //I CAN GENERATE A SCHEDULE. BOSH. cheers emily
        foreach (Job job in jobs)
        {
            var orderedJob = job.subJobs.OrderBy(s => s.operationId).ToList();
            foreach (SubJob subjob in orderedJob)
            {
                Schedule currentSchedule = schedules.Where(s => s.name == subjob.subdivision).ToList().FirstOrDefault(); //Should only ever return one schedule
                int maxScheduleEndTime;
                int maxSubjobEndTime;
                if (currentSchedule.subJobSchedule.Count == 0)
                {
                    maxScheduleEndTime = 0;
                }
                else
                {
                    maxScheduleEndTime = currentSchedule.subJobSchedule.Max(s => s.endJobTime);
                }
                maxSubjobEndTime = job.subJobs.Max(s => s.endJobTime);


                if (maxSubjobEndTime > maxScheduleEndTime)
                {
                    subjob.startJobTime = maxSubjobEndTime;
                } 
                else if (maxScheduleEndTime > maxSubjobEndTime)
                {
                    subjob.startJobTime = maxScheduleEndTime;
                }
                subjob.WriteEndJobTime();
                currentSchedule.subJobSchedule.Add(subjob);
            }
        }
        currentJobShopSchedule = schedules;
        //MAKE A DEEP COPY MAYBE


        //CheckValidity(jobs);
        return subjobs;
    }

    public bool CheckValidity()
    {
        //I  haven't tested this function

        //For each main job, we collate all it's subjobs into one array, then order it by operationId and start time. If they mismatch, then
        //the schedule must be invalid. We also check that each machine runs only one job at a time.
        for (int jobId = 1; jobId < jobs.Count; jobId++)
        {
            List<SubJob> subJobsOrderedByOpID;
            List<SubJob> subJobsOrderedByStartTime;
            foreach (var schedule in currentJobShopSchedule)
            {

                subJobsOrderedByOpID = schedule.subJobSchedule.OrderBy(x => x.operationId).ToList();
                subJobsOrderedByStartTime = schedule.subJobSchedule.OrderBy(x => x.startJobTime).ToList();
                for (int i = 0; i < schedule.subJobSchedule.Count; i++)
                {
                    if (subJobsOrderedByOpID[i] != subJobsOrderedByStartTime[i])
                    {
                        //If the operation order is different to the start time order then the solution is invalid
                        return false;
                    }
                }
            }
        }

        //Checks that the machine is running only one job at a time
        foreach (var schedule in currentJobShopSchedule)
        {
            foreach (var subJobToCheck in schedule.subJobSchedule)
            {
                foreach (var subJob in schedule.subJobSchedule)
                {
                    if (subJob != subJobToCheck)
                    //Avoids checking if the job conflicts with itself. Which it wouldn't actually
                    {
                        if (
                            //If another job ends between the current jobs duration then the solution is invalid
                            subJob.endJobTime > subJobToCheck.startJobTime && subJob.endJobTime < subJobToCheck.endJobTime ||
                            //If another job starts between the current jobs duration then the solution is invalid
                            subJob.startJobTime > subJobToCheck.startJobTime && subJob.endJobTime < subJobToCheck.endJobTime ||
                            //Jobs can also not have the same start time or end time in the schedule
                            subJob.startJobTime == subJobToCheck.startJobTime ||
                            subJob.endJobTime == subJobToCheck.endJobTime
                            )
                        {
                            return false;
                        }
                    }

                }
            }
        }

        //If the solution survives the checks then it is valid
        return true;
    }

    // Calculate the makespan of this schedule
    public int CalculateMakespan(JobShopSchedule jobShopSchedule)
    {
        //havent   tested if this works
        int total = 0;
        foreach (var schedule in jobShopSchedule.currentJobShopSchedule)
        {
            foreach (var subjob in schedule.subJobSchedule)
            {
                if (subjob.endJobTime > total)
                {
                    total = subjob.endJobTime;
                }
            }
        }
        return total;
    }

    //// Constructor to create a board with an existing route - this will be helpful for your crossover 😇
    //public TSPBoard(double[][] distances, int[] route)
    //{
    //    this.distances = distances;
    //    Route = new int[route.Length];
    //    Array.Copy(route, Route, route.Length);
    //}

    //// Randomise the route
    //private void ShuffleRoute()
    //{
    //    for (int i = 0; i < CityCount; i++)
    //    {
    //        int swapIndex = rand.Next(i, CityCount);
    //        (Route[i], Route[swapIndex]) = (Route[swapIndex], Route[i]);
    //    }
    //}

    //// Print route and distance
    //public void PrintRoute()
    //{
    //    Console.WriteLine("Route: " + string.Join(" -> ", Route.Concat(new[] { Route[0] })));
    //    Console.WriteLine($"Distance: {CalculateTotalDistance():0.##}\n");
    //}



}

//jack told me to make a list of operations in each job. i think he meant job not subjob. since in my code job is subjob.
//class Operation
//{
//    public int operationId;
//    public string operationName;
//}

class SubJob
{
    public int jobId;
    public int operationId;
    public string subdivision;
    public int processingTime;
    public int startJobTime;
    public int endJobTime;

    public void WriteEndJobTime()
    {
        //Not entirely necessary, but helps code readability
        this.endJobTime = startJobTime + processingTime;
    }
}

class Job
{
    public int jobId;

    public List<SubJob> subJobs = new List<SubJob>();
    public Job() { }
}

class Schedule
{
    public List<SubJob> subJobSchedule = new List<SubJob>();
    public string name;
    public int operationId;
    public Schedule(string name, int operationId)
    {
        this.name = name;
        this.operationId = operationId;
    }
}


class Solver
{

}

class Program
{
    // Loads the jobs from a csv into an array Job objects
    static List<SubJob> LoadSubJobs(string filename)
    {
        var lines = File.ReadAllLines(filename);
        List<SubJob> subJobs = new List<SubJob>();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split(',');
            SubJob subJob = new SubJob();
            subJob.jobId = Int32.Parse(parts[0]);
            subJob.operationId = Int32.Parse(parts[1]);
            subJob.subdivision = parts[2];
            subJob.processingTime = Int32.Parse(parts[3]);
            subJobs.Add(subJob);
            //Console.WriteLine(subJob.jobId + ", " + subJob.operationId + ", " + subJob.subdivision + ", " + subJob.processingTime);
        }
        return subJobs;
    }

    static List<Job> CreateJobs(List<SubJob> subJobs)
    {
        int numJobs = subJobs.Max(job => job.jobId);
        var Jobs = new List<Job>();
        for (int i = 1; i <= numJobs; i++)
        {
            Job job = new Job();
            job.jobId = i;
            foreach (SubJob subJob in subJobs)
            {
                if (subJob.jobId == job.jobId)
                {
                    job.subJobs.Add(subJob);
                }
            }
            Jobs.Add(job);
        }
        return Jobs;
    }

    static void Main()
    {
        // Load distance matrix from CSV
        string filename = "jobs_small.csv";
        List<SubJob> subJobs = LoadSubJobs(filename);
        List<Job> jobs = CreateJobs(subJobs);
        JobShopSchedule j = new JobShopSchedule(jobs, subJobs);
       // j.GenerateSchedule();

        Solver solver = new Solver();
        //solver.CalculateMakespan();
    }
}