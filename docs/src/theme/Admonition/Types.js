import React from 'react';
import DefaultAdmonitionTypes from '@theme-original/Admonition/Types';
import PraiseAdmonition from './Type/praise';

const AdmonitionTypes = {
  ...DefaultAdmonitionTypes,

  // Add all your custom admonition types here...
  // You can also override the default ones if you want
  'praise': PraiseAdmonition,
};

export default AdmonitionTypes;