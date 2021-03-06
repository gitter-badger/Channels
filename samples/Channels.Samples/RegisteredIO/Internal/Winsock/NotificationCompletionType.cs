// Copyright (c) Illyriad Games. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Channels.Samples.Internal.Winsock
{
    public enum NotificationCompletionType : int
    {
        Polling = 0,
        EventCompletion = 1,
        IocpCompletion = 2
    }
}
