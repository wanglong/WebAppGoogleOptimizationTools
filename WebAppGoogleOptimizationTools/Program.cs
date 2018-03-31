using Google.OrTools.ConstraintSolver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebAppGoogleOptimizationTools
{
    /// <summary>
    /// Program
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("Hello Google Optimization Tools World!");
            //RunLinearProgrammingExample("GLOP_LINEAR_PROGRAMMING");
            NurseScheduleMethod();

            Console.ReadKey();
        }

        /*
          护士调度问题
            在本例中，医院主管需要为四名护士创建一个周时间表，具体情况如下：

            每天分为早、中、晚三班轮班。
            在每一天，所有护士都被分配到不同的班次，除了有一名护士可以休息。
            每位护士每周工作五到六天。
            每个班次不会有超过两名护士在工作。
            如果一名护士某一天的班次是中班或晚班，她也必须在前一日或次日安排相同的班次。
            有两种方式来描述我们需要解决的问题：

            指派护士轮班
            将班次分配给护士
            事实证明，解决问题的最好方法是结合两种方式来求解。

            指派护士轮班
            下表显示了指派护士轮班视角的排班情况，这些护士被标记为A，B，C，D，换班，编号为0 - 3（其中0表示护士当天不工作）。
            				
            班次1	
            A   星期日
            B   星期一
            A   星期二
            A   星期三
            A   星期四
            A   星期五
            A   星期六
            班次2	
            C   星期日
            C   星期一
            C   星期二
            B   星期三
            B   星期四
            B   星期五
            B   星期六
            班次3	
            D   星期日
            D   星期一
            D   星期二
            D   星期三
            C   星期四
            C   星期五
            D   星期六
            将班次分配给护士
            下表显示了将班次分配给护士视角的排班情况。

 	            星期日	星期一	星期二	星期三	星期四	星期五	星期六
            护士A	1	0	1	1	1	1	1
            护士B	0	1	0	2	2	2	2
            护士C	2	2	2	0	3	3	0
            护士D	3	3	3	3	0	0	3

            基本概念：

            IntVar是约束求解中使用最多的变量形式，一般约束问题中变化的对象都应该定义为一个类似在一定范围内整形数值的变量。
            solver.MakeIntVar是创建约束求解中变量的方法，约束求解一定会定义一些可变化的对象，一般都需要转化成数值类型。
            solver.Add是添加若干约束条件的方法。
            solver.MakePhase定义了求解的目标以及求解的取值策略。
            solver.Solve进行求解，并对指定的集合赋值。
            solver.MakeAllSolutionCollector表示获取解的集合对象。
            */
        /// <summary>
        ///  护士排班计划
        /// </summary>
        private static void NurseScheduleMethod()
        {
            // 创建约束求解器. 
            var solver = new Solver("schedule_shifts");
            var num_nurses = 4;
            var num_shifts = 4;  // 班次数定为4，这样序号为0的班次表示是休息的班。
            var num_days = 7;

            // [START]
            //shift和nurse分别来表示班次和护士 。
            // 创建班次变量
            var shifts = new Dictionary<(int, int), IntVar>();

            foreach (var j in Enumerable.Range(0, num_nurses))
            {
                foreach (var i in Enumerable.Range(0, num_days))
                {
                    // shifts[(j, i)]表示护士j在第i天的班次，可能的班次的编号范围是:[0, num_shifts)
                    shifts[(j, i)] = solver.MakeIntVar(0, num_shifts - 1, string.Format("shifts({0},{1})", j, i));
                }
            }

            // 将变量集合转成扁平化数组
            var shifts_flat = (from j in Enumerable.Range(0, num_nurses)
                               from i in Enumerable.Range(0, num_days)
                               select shifts[(j, i)]).ToArray();

            /*
             shifts和nurses两个对象含义如下：
            shifts[(j, i)]表示护士j在第i天的班次，可能的班次的编号范围是:[0, num_shifts)。
            nurses[(j, i)]表示班次j在第i天的当班护士，可能的护士的编号范围是:[0, num_nurses)。
            shifts_flat是将shifts的Values简单地处理成扁平化，
            后面直接用于当参数传给约束求解器solver以指定需要求解的变量。
             */
            // 创建护士变量
            var nurses = new Dictionary<(int, int), IntVar>();

            foreach (var j in Enumerable.Range(0, num_shifts))
            {
                foreach (var i in Enumerable.Range(0, num_days))
                {
                    // nurses[(j, i)]表示班次j在第i天的当班护士，可能的护士的编号范围是:[0, num_nurses)
                    nurses[(j, i)] = solver.MakeIntVar(0, num_nurses - 1, string.Format("shift{0} day{1}", j, i));
                }
            }

            /*
             将每一天的nurses单独列出来，按照编号顺序扁平化成一个数组对象，
             s.IndexOf(nurses_for_day)是一种OR-Tools要求的特定用法，
             相当于nurses_for_day[s]求值。这里利用了s的值恰好是在nurses_for_day中对应nurse的编号。
             注意这里的两层foreach循环，v外层不能互换，必须是现在这样，内层循环的主体对象与shifts_flat一致。
             */
            // 定义shifts和nurses之前的关联关系
            foreach (var day in Enumerable.Range(0, num_days))
            {
                var nurses_for_day = (from j in Enumerable.Range(0, num_shifts)
                                      select nurses[(j, day)]).ToArray();
                foreach (var j in Enumerable.Range(0, num_nurses))
                {
                    var s = shifts[(j, day)];
                    // s.IndexOf(nurses_for_day)相当于nurses_for_day[s]
                    // 这里利用了s的值恰好是在nurses_for_day中对应nurse的编号
                    solver.Add(s.IndexOf(nurses_for_day) == j);
                }
            }

            /*
             定义护士在不同的班次当班约束
             AllDifferent方法是OR-Tools定义约束的方法之一，表示指定的IntVar数组在进行计算时受唯一性制约。
             满足每一天的当班护士不重复，即每一天的班次不会出现重复的护士的约束条件，
             同样每一个护士每天不可能同时轮值不同的班次。
             */
            // 满足每一天的当班护士不重复，每一天的班次不会出现重复的护士的约束条件
            // 同样每一个护士每天不可能同时轮值不同的班次
            foreach (var i in Enumerable.Range(0, num_days))
            {
                solver.Add((from j in Enumerable.Range(0, num_nurses)
                            select shifts[(j, i)]).ToArray().AllDifferent());
                solver.Add((from j in Enumerable.Range(0, num_shifts)
                            select nurses[(j, i)]).ToArray().AllDifferent());
            }

            /*
             定义护士每周当班次数的约束
             Sum方法是OR-Tools定义运算的方法之一。注意shifts[(j, i)] > 0运算被重载过，
             其返回类型是WrappedConstraint而不是默认的bool。满足每个护士在一周范围内只出现[5, 6]次。
             */
            // 满足每个护士在一周范围内只出现[5, 6]次
            foreach (var j in Enumerable.Range(0, num_nurses))
            {
                solver.Add((from i in Enumerable.Range(0, num_days)
                            select shifts[(j, i)] > 0).ToArray().Sum() >= 5);
                solver.Add((from i in Enumerable.Range(0, num_days)
                            select shifts[(j, i)] > 0).ToArray().Sum() <= 6);
            }

            /*
             定义每个班次在一周内当班护士人数的约束
             Max方法是OR-Tools定义运算的方法之一，表示对指定的IntVar数组求最大值。
             注意MakeBoolVar方法返回类型是IntVar而不是默认的bool，works_shift, j)]为True
             表示护士i在班次j一周内至少要有1次，BoolVar类型的变量最终取值是0或1，同样也表示了False或True。
             满足每个班次一周内不会有超过两名护士当班工作。
             */
            // 创建一个工作的变量，works_shift[(i, j)]为True表示护士i在班次j一周内至少要有1次
            // BoolVar类型的变量最终取值是0或1，同样也表示了False或True
            var works_shift = new Dictionary<(int, int), IntVar>();

            foreach (var i in Enumerable.Range(0, num_nurses))
            {
                foreach (var j in Enumerable.Range(0, num_shifts))
                {
                    works_shift[(i, j)] = solver.MakeBoolVar(string.Format("nurse%d shift%d", i, j));
                }
            }

            foreach (var i in Enumerable.Range(0, num_nurses))
            {
                foreach (var j in Enumerable.Range(0, num_shifts))
                {
                    // 建立works_shift与shifts的关联关系
                    // 一周内的值要么为0要么为1，所以Max定义的约束是最大值，恰好也是0或1，1表示至少在每周轮班一天
                    solver.Add(works_shift[(i, j)] == (from k in Enumerable.Range(0, num_days)
                                                       select shifts[(i, k)].IsEqual(j)).ToArray().Max());
                }
            }

            // 对于每个编号不为0的shift, 满足至少每周最多同一个班次2个护士当班
            foreach (var j in Enumerable.Range(1, num_shifts - 1))
            {
                solver.Add((from i in Enumerable.Range(0, num_nurses)
                            select works_shift[(i, j)]).ToArray().Sum() <= 2);
            }

            // 满足中班或晚班的护士前一天或后一天也是相同的班次
            // 用nurses的key中Tuple类型第1个item的值表示shift为2或3
            // shift为1表示早班班次，shift为0表示休息的班次
            solver.Add(solver.MakeMax(nurses[(2, 0)] == nurses[(2, 1)], nurses[(2, 1)] == nurses[(2, 2)]) == 1);
            solver.Add(solver.MakeMax(nurses[(2, 1)] == nurses[(2, 2)], nurses[(2, 2)] == nurses[(2, 3)]) == 1);
            solver.Add(solver.MakeMax(nurses[(2, 2)] == nurses[(2, 3)], nurses[(2, 3)] == nurses[(2, 4)]) == 1);
            solver.Add(solver.MakeMax(nurses[(2, 3)] == nurses[(2, 4)], nurses[(2, 4)] == nurses[(2, 5)]) == 1);
            solver.Add(solver.MakeMax(nurses[(2, 4)] == nurses[(2, 5)], nurses[(2, 5)] == nurses[(2, 6)]) == 1);
            solver.Add(solver.MakeMax(nurses[(2, 5)] == nurses[(2, 6)], nurses[(2, 6)] == nurses[(2, 0)]) == 1);
            solver.Add(solver.MakeMax(nurses[(2, 6)] == nurses[(2, 0)], nurses[(2, 0)] == nurses[(2, 1)]) == 1);

            solver.Add(solver.MakeMax(nurses[(3, 0)] == nurses[(3, 1)], nurses[(3, 1)] == nurses[(3, 2)]) == 1);
            solver.Add(solver.MakeMax(nurses[(3, 1)] == nurses[(3, 2)], nurses[(3, 2)] == nurses[(3, 3)]) == 1);
            solver.Add(solver.MakeMax(nurses[(3, 2)] == nurses[(3, 3)], nurses[(3, 3)] == nurses[(3, 4)]) == 1);
            solver.Add(solver.MakeMax(nurses[(3, 3)] == nurses[(3, 4)], nurses[(3, 4)] == nurses[(3, 5)]) == 1);
            solver.Add(solver.MakeMax(nurses[(3, 4)] == nurses[(3, 5)], nurses[(3, 5)] == nurses[(3, 6)]) == 1);
            solver.Add(solver.MakeMax(nurses[(3, 5)] == nurses[(3, 6)], nurses[(3, 6)] == nurses[(3, 0)]) == 1);
            solver.Add(solver.MakeMax(nurses[(3, 6)] == nurses[(3, 0)], nurses[(3, 0)] == nurses[(3, 1)]) == 1);

            //定义约束求解器的使用
            // 将变量集合设置为求解的目标，Solver有一系列的枚举值，可以指定求解的选择策略。
            var db = solver.MakePhase(shifts_flat, Solver.CHOOSE_FIRST_UNBOUND, Solver.ASSIGN_MIN_VALUE);

            //执行求解计算并显示结果
            // 创建求解的对象
            var solution = solver.MakeAssignment();
            solution.Add(shifts_flat);
            var collector = solver.MakeAllSolutionCollector(solution);

            solver.Solve(db, new[] { collector });
            Console.WriteLine("Solutions found: {0}", collector.SolutionCount());
            Console.WriteLine("Time: {0}ms", solver.WallTime());
            Console.WriteLine();

            // 显示一些随机的结果
            var a_few_solutions = new[] { 340, 2672, 7054 };

            foreach (var sol in a_few_solutions)
            {
                Console.WriteLine("Solution number {0}", sol);

                foreach (var i in Enumerable.Range(0, num_days))
                {
                    Console.WriteLine("Day {0}", i);
                    foreach (var j in Enumerable.Range(0, num_nurses))
                    {
                        Console.WriteLine("Nurse {0} assigned to task {1}", j, collector.Value(sol, shifts[(j, i)]));
                    }
                    Console.WriteLine();
                }
            }
        }
        
        /*
        /// <summary>
        /// 体验测试案例
        /// RunLinearProgrammingExample
        /// </summary>
        /// <param name="solverType"></param>
        private static void RunLinearProgrammingExample(String solverType)
        {
            Solver solver = Solver.CreateSolver("IntegerProgramming", solverType);
            Variable x = solver.MakeNumVar(0.0, 1.0, "x");
            Variable y = solver.MakeNumVar(0.0, 2.0, "y");
            Objective objective = solver.Objective();
            objective.SetCoefficient(x, 1);
            objective.SetCoefficient(y, 1);
            objective.SetMaximization();
            solver.Solve();
            Console.WriteLine("Solution:");
            Console.WriteLine("x = " + x.SolutionValue());
            Console.WriteLine("y = " + y.SolutionValue());
        }
        */
    }
}
