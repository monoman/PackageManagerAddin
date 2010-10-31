using System;

namespace NuGet.Test {
    public static class Capture {
        public static Exception Exception(Action act) {
            Exception ex = null;
            try {
                act();
            }
            catch (Exception exc) {
                ex = exc;
            }

            return ex;
        }
    }
}
