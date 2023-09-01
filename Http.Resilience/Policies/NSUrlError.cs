namespace Http.Resilience.Policies
{
    public enum NSUrlError
    {
        DownloadDecodingFailedToComplete = -3007, // 0xFFFFF441
        DownloadDecodingFailedMidStream = -3006, // 0xFFFFF442
        CannotMoveFile = -3005, // 0xFFFFF443
        CannotRemoveFile = -3004, // 0xFFFFF444
        CannotWriteToFile = -3003, // 0xFFFFF445
        CannotCloseFile = -3002, // 0xFFFFF446
        CannotOpenFile = -3001, // 0xFFFFF447
        CannotCreateFile = -3000, // 0xFFFFF448
        CannotLoadFromNetwork = -2000, // 0xFFFFF830
        ClientCertificateRequired = -1206, // 0xFFFFFB4A
        ClientCertificateRejected = -1205, // 0xFFFFFB4B
        ServerCertificateNotYetValid = -1204, // 0xFFFFFB4C
        ServerCertificateHasUnknownRoot = -1203, // 0xFFFFFB4D
        ServerCertificateUntrusted = -1202, // 0xFFFFFB4E
        ServerCertificateHasBadDate = -1201, // 0xFFFFFB4F
        SecureConnectionFailed = -1200, // 0xFFFFFB50
        FileOutsideSafeArea = -1104, // 0xFFFFFBB0
        DataLengthExceedsMaximum = -1103, // 0xFFFFFBB1
        NoPermissionsToReadFile = -1102, // 0xFFFFFBB2
        FileIsDirectory = -1101, // 0xFFFFFBB3
        FileDoesNotExist = -1100, // 0xFFFFFBB4
        AppTransportSecurityRequiresSecureConnection = -1022, // 0xFFFFFC02
        RequestBodyStreamExhausted = -1021, // 0xFFFFFC03
        DataNotAllowed = -1020, // 0xFFFFFC04
        CallIsActive = -1019, // 0xFFFFFC05
        InternationalRoamingOff = -1018, // 0xFFFFFC06
        CannotParseResponse = -1017, // 0xFFFFFC07
        CannotDecodeContentData = -1016, // 0xFFFFFC08
        CannotDecodeRawData = -1015, // 0xFFFFFC09
        ZeroByteResource = -1014, // 0xFFFFFC0A
        UserAuthenticationRequired = -1013, // 0xFFFFFC0B
        UserCancelledAuthentication = -1012, // 0xFFFFFC0C
        BadServerResponse = -1011, // 0xFFFFFC0D
        RedirectToNonExistentLocation = -1010, // 0xFFFFFC0E
        NotConnectedToInternet = -1009, // 0xFFFFFC0F
        ResourceUnavailable = -1008, // 0xFFFFFC10
        HTTPTooManyRedirects = -1007, // 0xFFFFFC11
        DNSLookupFailed = -1006, // 0xFFFFFC12
        NetworkConnectionLost = -1005, // 0xFFFFFC13
        CannotConnectToHost = -1004, // 0xFFFFFC14
        CannotFindHost = -1003, // 0xFFFFFC15
        UnsupportedURL = -1002, // 0xFFFFFC16
        TimedOut = -1001, // 0xFFFFFC17
        BadURL = -1000, // 0xFFFFFC18
        Cancelled = -999, // 0xFFFFFC19
        BackgroundSessionWasDisconnected = -997, // 0xFFFFFC1B
        BackgroundSessionInUseByAnotherProcess = -996, // 0xFFFFFC1C
        BackgroundSessionRequiresSharedContainer = -995, // 0xFFFFFC1D
        Unknown = -1, // 0xFFFFFFFF
    }
}