// Runner: invoke any exam modules' `run` or `main` entry points if they exist.
// For more information see https://aka.ms/fsharp-console-apps
open System
open System.Reflection

printfn "Runner starting..."

let assembly = Assembly.GetExecutingAssembly()
let isFSharpModule (t: Type) =
  // F# modules are compiled to abstract sealed classes
  t.IsAbstract && t.IsSealed

let findEntryMethod (t: Type) =
  let methods = t.GetMethods(BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Static)
  methods
  |> Array.tryFind (fun m ->
    let n = m.Name.ToLowerInvariant()
    n = "run" || n = "main"
  )

let invokeIfPresent (t: Type) =
  match findEntryMethod t with
  | None ->
    printfn "No entry method found in module: %s" t.FullName
  | Some m ->
    try
      printfn "Invoking %s.%s..." t.FullName m.Name
      // Choose invocation based on parameter info: no params or single unit param
      let parameters = m.GetParameters()
      let res =
        match parameters with
        | [||] -> m.Invoke(null, [||])
        | [| p |] when p.ParameterType = typeof<unit> -> m.Invoke(null, [| box () |])
        | _ ->
          printfn "-> Skipping %s.%s: unsupported parameter signature" t.FullName m.Name
          null

      printfn "-> Completed %s.%s, result = %A" t.FullName m.Name res
    with ex ->
      printfn "-> Exception from %s.%s: %s" t.FullName m.Name (ex.ToString())

// Ask user which sets to run
printfn "What do you want to run?"
printfn "  1) exams"
printfn "  2) exercises"
printfn "  3) both (default)"
printf "> "
let choice = Console.ReadLine()

let parseChoice (s: string) =
  if String.IsNullOrWhiteSpace s then 3
  else
    match s.Trim().ToLowerInvariant() with
    | "1" | "exams" -> 1
    | "2" | "exercises" -> 2
    | "3" | "both" -> 3
    | _ -> 3

let selection = parseChoice choice
let wantExams = selection = 1 || selection = 3
let wantExercises = selection = 2 || selection = 3

let matchesCategory (t: Type) =
  let name = t.Name
  if wantExams && name.StartsWith("Exam", StringComparison.InvariantCultureIgnoreCase) then true
  elif wantExercises && name.StartsWith("Exercise", StringComparison.InvariantCultureIgnoreCase) then true
  else false

assembly.GetTypes()
|> Array.filter isFSharpModule
|> Array.filter matchesCategory
|> Array.sortBy (fun t -> t.FullName)
|> Array.iter invokeIfPresent

printfn "Runner finished."
