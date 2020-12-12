namespace AspFeat

type ProblemDetails =
    { Type: string option
      Title: string option
      Detail: string option
      Status: int option
      Instance: string option }

module ProblemDetails =
    let empty =
        { Type = None
          Title = None
          Detail = None
          Status = None
          Instance = None }
    let create status title detail =
        { empty with
            Status = Some status
            Title = Some title
            Detail = Some detail }
