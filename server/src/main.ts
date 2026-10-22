// Nakama loads build/index.js and calls the global InitModule once at startup.
// Register RPCs, hooks and match handlers here as milestones need them.
function InitModule(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, initializer: nkruntime.Initializer) {
  logger.info('wuxia module loaded');
}
